using System;
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
            var lockKey = Guid.NewGuid().ToString();
            var redisPipeValue = new RedisPipeValue(PipeInfo.Create("unheld", "a"), lockKey, "asdf", true);

            Action act = () => redisTaskFunnel.LockRelease(redisPipeValue, true);
            act.Should().Throw<ApplicationException>();
        }

        [Fact]
        public void ReleaseLockExtend()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var lockKey = Guid.NewGuid().ToString();
            var redisPipeValue = new RedisPipeValue(PipeInfo.Create("lock", "a"), lockKey, "asdf", true);
            var extended = redisTaskFunnel.LockExtend(redisPipeValue, TimeSpan.FromMinutes(1));
            extended.Should().BeFalse();
        }

        [Fact]
        public void LockNotAcquiredFor()
        {
            var redisTaskFunnel = CreateRedisTaskFunnel();
            var read = redisTaskFunnel.TryReadMessage(true, PipeInfo.Create("emptyqueue", "emptypipe"), TimeSpan.FromSeconds(10));
            read.LockValue.Should().BeNull();
            read.Value.Should().BeNull();
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
            var sent = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, "body", Int32.MaxValue,
                TimeSpan.FromMinutes(1));
            sent.sent.Should().BeTrue();
            sent.clients.Should().BeFalse();

            var read = redisTaskFunnel.TryReadMessage(true, PipeInfo.Create(parentPipeName, childPipeName), TimeSpan.FromSeconds(1));
            read.Should().NotBeNull();
            read.LockValue.Should().NotBeNull();
            read.Value.Should().NotBeNull();

            //try to release the lock without the correct key
            var redisPipeValue = new RedisPipeValue(PipeInfo.Create(parentPipeName, childPipeName), "body", Guid.NewGuid().ToString(), true);
            var badExtend = redisTaskFunnel.LockExtend(redisPipeValue, TimeSpan.FromSeconds(1));
            badExtend.Should().BeFalse();
            Action badRelease = () => redisTaskFunnel.LockRelease(redisPipeValue, true);
            badRelease.Should().Throw<ApplicationException>();

            var extended = redisTaskFunnel.LockExtend(read, TimeSpan.FromSeconds(1));
            extended.Should().BeTrue();

            var released = redisTaskFunnel.LockRelease(read, true);
            released.Should().BeTrue();
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
            services.AddLogging();
            return services;
        }
    }
}
