using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace MsgNThen.Redis.Tests
{
    public class TaskExecutorTests
    {

        /// <summary>
        /// Use redisTaskFunnel to send messages that are picked up by the mocked taskExecutor which increments tasksCalled.
        ///  - Send 3 Messages
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ReleaseUnheldLock()
        {
            var parentPipeName = "ConsoleWriter";
            var serviceCollection = RedisTaskFunnelsTests.CreateServiceCollection();
            var taskExecutor = Substitute.For<ITaskExecutor>();
            int tasksCalled = 0;
            taskExecutor.When(a=>a.Execute(Arg.Any<PipeInfo>(), Arg.Any<RedisPipeValue>())).Do(x=> Interlocked.Increment(ref tasksCalled));
            serviceCollection.AddSingleton(new Dictionary<string, ITaskExecutor>{ { parentPipeName , taskExecutor } });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var redisTaskFunnel = serviceProvider.GetRequiredService<IRedisTaskFunnel>();
            var taskReader = serviceProvider.GetRequiredService<ITaskReader>();
            var cancellationTokenSource = new CancellationTokenSource();
            var taskReaderTask = Task.Factory.StartNew(()=> taskReader.Start(TimeSpan.FromMinutes(2), cancellationTokenSource.Token));

            //give the task reader a chance to get running
            await Task.Delay(TimeSpan.FromSeconds(.1));
            tasksCalled = 0;

            var childPipeName = "kittens";
            var(sent1, clients1) = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent1.Should().BeTrue();
            clients1.Should().BeTrue();
            //wait for it to process all the messages
            await Task.Delay(TimeSpan.FromSeconds(.1));
            tasksCalled.Should().Be(1);

            var (sent2, clients2) = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent2.Should().BeTrue();
            clients2.Should().BeTrue();
            var (sent3, clients3) = redisTaskFunnel.TrySendMessage(parentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent3.Should().BeTrue();
            clients3.Should().BeTrue();
            //wait for it to process all the messages
            await Task.Delay(TimeSpan.FromSeconds(.1));
            tasksCalled.Should().Be(3);

            cancellationTokenSource.Cancel();
            try
            {
                await taskReaderTask;
            }
            catch (OperationCanceledException)
            {
                //yes, we cancelled it.
            }
        }
    }
}
