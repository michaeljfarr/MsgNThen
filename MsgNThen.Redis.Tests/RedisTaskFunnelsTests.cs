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
        public void LockNotAcquiredFor()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var read = redisTaskFunnel.TryReadMessageBatch(true, PipeInfo.Create("emptyqueue", "emptypipe"), TimeSpan.FromSeconds(10), 1);
            read.HoldingList.Should().BeNull();
            read.RedisValues.Should().BeEmpty();
        }

        [Fact]
        public void TestSendReadAndRelease()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var parentPipeName = "ReleaseSendLock";
            var childPipeName = Guid.NewGuid().ToString();
            //do twice to ensure that lock is released properly after first read
            SendReadAndRelease(redisTaskFunnel, parentPipeName, childPipeName);
            SendReadAndRelease(redisTaskFunnel, parentPipeName, childPipeName);
        }

        private static void SendReadAndRelease(IRedisTaskFunnel redisTaskFunnel, string parentPipeName, string childPipeName)
        {
            //send a message
            var sent = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, "body", Int32.MaxValue,
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
