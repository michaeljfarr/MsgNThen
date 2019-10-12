using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Redis.Abstractions;
using Xunit;

namespace MsgNThen.Redis.Tests
{
    public class RedisTaskFunnelsTests
    {
        //todo: test that child pipes decay when they are empty.
        [Fact]
        public void ReleaseUnheldLock()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var msgValue = Guid.NewGuid().ToByteArray();
            var pipeInfo = PipeInfo.Create("unheld", "a");
            redisTaskFunnel.DestroyChildPipe(pipeInfo);
            var redisPipeValue = new RedisPipeValue(pipeInfo, msgValue, "asdf", true);

            //indicating a success does nothing if the hash doesn't exist
            redisTaskFunnel.AfterExecute(redisPipeValue, true);
            var readMessage = redisTaskFunnel.TryReadMessageBatch(false, pipeInfo, TimeSpan.FromMinutes(1), 1);
            readMessage.RedisValues.Should().BeEmpty();

            //indicating a failure resubmits the message 
            redisTaskFunnel.AfterExecute(redisPipeValue, false);
            readMessage = redisTaskFunnel.TryReadMessageBatch(false, pipeInfo, TimeSpan.FromMinutes(1), 1);
            readMessage.Should().NotBeNull();
            var mesg = readMessage.RedisPipeValues.FirstOrDefault();
            mesg?.Value.Should().BeEquivalentTo(msgValue);
            redisTaskFunnel.DestroyChildPipe(pipeInfo);
            redisTaskFunnel.AfterExecuteBatch(readMessage);
        }

        [Fact]
        public void ReleaseLockExtend()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var lockKey = Guid.NewGuid().ToString();
            var pipeInfo = PipeInfo.Create("lock", "a");
            var redisPipeValue = new RedisPipeValue(pipeInfo, lockKey, "asdf", true);
            var extended = redisTaskFunnel.RetainHoldingList(redisPipeValue, TimeSpan.FromMinutes(1));
            extended.Should().BeFalse();
            redisTaskFunnel.DestroyChildPipe(pipeInfo);
        }

        [Fact]
        public void ReadFromNonExistantOrEmptyQueue()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var read = redisTaskFunnel.TryReadMessageBatch(true, PipeInfo.Create("emptyqueue", "emptypipe"), TimeSpan.FromSeconds(10), 1);
            read.HoldingList.Should().BeNull();
            read.RedisValues.Should().BeEmpty();
        }

        /// <summary>
        /// This tests the main happy path of this redisTaskFunnel (the low level implementation)
        ///   - Send a message
        ///   - Read it (with peek) and check it is the same message
        ///   - Release the Holding List.
        /// </summary>
        [Fact]
        public void TestSendReadAndRelease()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var parentPipeName = "ReleaseSendLock";
            var childPipeName = Guid.NewGuid().ToString();
            //do twice to ensure correct tidyup takes place
            SendReadAndRelease(redisTaskFunnel, parentPipeName, childPipeName);
            SendReadAndRelease(redisTaskFunnel, parentPipeName, childPipeName);
        }

        private static void SendReadAndRelease(IRedisTaskFunnel redisTaskFunnel, string parentPipeName, string childPipeName)
        {
            //send a message
            var messageBody = "body";
            var sent = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, messageBody, Int32.MaxValue,
                TimeSpan.FromMinutes(1));
            sent.sent.Should().BeTrue();
            //sent.clients.Should().BeFalse();

            //read the batch
            var read = redisTaskFunnel.TryReadMessageBatch(true, PipeInfo.Create(parentPipeName, childPipeName), TimeSpan.FromSeconds(1), 1);
            read.Should().NotBeNull();
            read.HoldingList.Should().NotBeNull();
            read.RedisValues.Should().NotBeEmpty();
            read.RedisValues.First().HasValue.Should().BeTrue();
            var actualRedisPipeValue = read.RedisPipeValues.First();
            actualRedisPipeValue.ValueString.Should().Be(messageBody);

            //try to release the lock without the wrong holdingListName
            var redisPipeValue = new RedisPipeValue(PipeInfo.Create(parentPipeName, childPipeName), "body", Guid.NewGuid().ToString(), true);
            var badExtend = redisTaskFunnel.RetainHoldingList(redisPipeValue, TimeSpan.FromSeconds(1));
            badExtend.Should().BeFalse();
            redisTaskFunnel.AfterExecute(redisPipeValue, true);

            //retain with the correct name
            var extended = redisTaskFunnel.RetainHoldingList(read, TimeSpan.FromSeconds(1));
            extended.Should().BeTrue();

            //complete the message and the batch
            redisTaskFunnel.AfterExecute(actualRedisPipeValue, true);
            redisTaskFunnel.AfterExecuteBatch(read);

            //now check the holding list doesn't exist any more.
            extended = redisTaskFunnel.RetainHoldingList(read, TimeSpan.FromSeconds(1));
            extended.Should().BeFalse();
        }


        [Fact]
        public void TestSendAndRecover()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var parentPipeName = "SendAndRecover";
            var childPipeName = Guid.NewGuid().ToString();

            //send 2 messages
            var messageBody1 = "body1";
            var sent = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, messageBody1, Int32.MaxValue,
                TimeSpan.FromMinutes(1));
            sent.sent.Should().BeTrue();
            var messageBody2 = "body2";
            sent = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, messageBody2, Int32.MaxValue,
                TimeSpan.FromMinutes(1));
            sent.sent.Should().BeTrue();
            //sent.clients.Should().BeFalse();

            //read the batch
            var pipeInfo = PipeInfo.Create(parentPipeName, childPipeName);
            var read = redisTaskFunnel.TryReadMessageBatch(true, pipeInfo, TimeSpan.FromSeconds(1), 2);
            read.Should().NotBeNull();
            read.HoldingList.Should().NotBeNull();
            read.RedisValues.Count.Should().Be(2);
            read.RedisValues.First().HasValue.Should().BeTrue();
            read.RedisValues.Skip(1).First().HasValue.Should().BeTrue();
            read.RedisPipeValues.Any(a => a.ValueString == messageBody1).Should().BeTrue();
            read.RedisPipeValues.Any(a => a.ValueString == messageBody2).Should().BeTrue();

            //recover batch (redeliver its messages) 
            redisTaskFunnel.RecoverBatch(read.HoldingList).Should().BeTrue();
            read = redisTaskFunnel.TryReadMessageBatch(true, pipeInfo, TimeSpan.FromSeconds(1), 2);
            read.Should().NotBeNull();
            read.HoldingList.Should().NotBeNull();
            read.RedisValues.Count.Should().Be(2);
            read.RedisValues.First().HasValue.Should().BeTrue();
            read.RedisValues.Skip(1).First().HasValue.Should().BeTrue();
            var actualRedisPipeValue = read.RedisPipeValues.First();
            read.RedisPipeValues.Any(a => a.ValueString == messageBody1).Should().BeTrue();
            read.RedisPipeValues.Any(a => a.ValueString == messageBody2).Should().BeTrue();
        }



        public static IRedisTaskFunnel CreateRedisTaskFunnel()
        {
            var serviceProvider = CreateServiceProvider();
            var redisTaskFunnel = serviceProvider.GetRequiredService<IRedisTaskFunnel>();
            return redisTaskFunnel;
        }

        public static ServiceProvider CreateServiceProvider()
        {
            var services = CreateServiceCollection();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        public static ServiceCollection CreateServiceCollection()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("settings.json");

            var configuration = builder.Build();

            var services = new ServiceCollection();
            services.ConfigureRedis(configuration);
            services.AddRedisFactory();
            services.AddRedisMonitor();
            services.AddRedisPipework();
            services.AddRedisNThenEventHandler();
            services.AddLogging();
            return services;
        }
    }
}
