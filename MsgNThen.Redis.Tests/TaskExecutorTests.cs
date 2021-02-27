using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Redis.Abstractions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace MsgNThen.Redis.Tests
{
    [Collection("NonParallelTests")]
    public class TaskExecutorTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private int _tasksCalled;
        private readonly IRedisTaskFunnel _redisTaskFunnel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _taskReaderTask;
        private const string ParentPipeName = "ConsoleWriter";
        private bool Block = false;

        public TaskExecutorTests(ITestOutputHelper output)
        {
            _output = output;
            var serviceCollection = RedisTaskFunnelsTests.CreateServiceCollection();
            var taskExecutor = Substitute.For<ITaskExecutor>();
            taskExecutor.When(a => a.Execute(Arg.Any<PipeInfo>(), Arg.Any<RedisPipeValue>()))
                .Do(x =>
                {
                    while (Block)
                    {
                        Thread.Sleep(100);
                    }

                    Interlocked.Increment(ref _tasksCalled);
                });
            serviceCollection.AddSingleton(new Dictionary<string, ITaskExecutor> { { ParentPipeName, taskExecutor } });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _redisTaskFunnel = serviceProvider.GetRequiredService<IRedisTaskFunnel>();
            var taskReader = serviceProvider.GetRequiredService<ITaskReader>();
            _cancellationTokenSource = new CancellationTokenSource();
            _taskReaderTask = Task.Factory.StartNew(() => taskReader.Start(TimeSpan.FromMinutes(2), _cancellationTokenSource.Token));
        }
        /// <summary>
        /// Initial redis performance is Write 805.9/s,  Read 653.5/s with no batching on either the read or write side.
        ///  - also, the measurement of read throughput is an underestimate because it is running along side the writer
        /// Second implementation stats were (note: simply interpret these as the batch size doesn't matter).
        ///    read batching of 1: Write 1114.1/s,  Read 845.9/s
        ///    read batching of 2: Write 1075.8/s,  Read 829.7/s
        ///    read batching of 5: Write 1090.2/s,  Read 826.4/s
        /// </summary>
        [Theory]
        [InlineData(10000, 100)]
        public async Task PerformanceTestSimpleRedisMessaging(int numMessages, int expectedPerSec)
        {
            //give the task reader a chance to get running
            await Task.Delay(TimeSpan.FromSeconds(.1));
            Block = true;
            _tasksCalled = 0;
            var childPipeName = "perf";
            var swWriter = Stopwatch.StartNew();
            bool sent = false;
                bool clients = false;;
            for (int i = 0; i < numMessages; i++)
            {
                (sent, clients) = _redisTaskFunnel.TrySendMessage(ParentPipeName, childPipeName, "body", (int)(numMessages*1.5), TimeSpan.FromDays(1));
                if (!sent)
                {
                    //lets assume that the queue is full, so there are already enough messages to read
                    //this will put off the throughput numbers, so take care to clear redis before relying on these numbers.
                    break;
                }

            }
            //sent.Should().BeTrue();
            //clients.Should().BeTrue();
            swWriter.Stop();
            Block = false;
            var swReader = Stopwatch.StartNew();


            //wait for it to process all the messages
            for (int i = 0; i < 100 && _tasksCalled<(numMessages*0.75); i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(.1));
            }

            var numMessagesReceived = _tasksCalled;
            swReader.Stop();

            var writePerSecond = numMessages/ swWriter.Elapsed.TotalSeconds;
            var readPerSecond = numMessagesReceived / swWriter.Elapsed.TotalSeconds;
            _output.WriteLine($"Done in {swWriter.Elapsed.TotalSeconds + swReader.Elapsed.TotalSeconds:F1}: Write {writePerSecond:F1}/s,  Read {readPerSecond:F1}/s");
            writePerSecond.Should().BeGreaterOrEqualTo(expectedPerSec);
            readPerSecond.Should().BeGreaterOrEqualTo(expectedPerSec);

            //drain all messages
            int lastCount = 0;
            for (int i = 0; i < 100 && lastCount!= _tasksCalled; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(.1));
                lastCount = _tasksCalled;
            }
        }

        /// <summary>
        /// Use redisTaskFunnel to send messages that are picked up by the mocked taskExecutor which increments tasksCalled.
        ///  - Send 3 Messages
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestSimpleRedisMessaging()
        {
            //give the task reader a chance to get running
            await Task.Delay(TimeSpan.FromSeconds(1));
            _tasksCalled = 0;

            var childPipeName = "kittens";
            var (sent1, clients1) = _redisTaskFunnel.TrySendMessage(ParentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent1.Should().BeTrue();
            clients1.Should().BeTrue();
            //wait for it to process all the messages
            await Task.Delay(TimeSpan.FromSeconds(.1));
            _tasksCalled.Should().Be(1);

            var (sent2, clients2) = _redisTaskFunnel.TrySendMessage(ParentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent2.Should().BeTrue();
            clients2.Should().BeTrue();
            var (sent3, clients3) = _redisTaskFunnel.TrySendMessage(ParentPipeName, childPipeName, "body", 200, TimeSpan.FromDays(1));
            sent3.Should().BeTrue();
            clients3.Should().BeTrue();
            //wait for it to process all the messages
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            _tasksCalled.Should().Be(3);

        }


        private async Task Shutdown()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _taskReaderTask;
            }
            catch (OperationCanceledException)
            {
                //yes, we cancelled it.
            }
        }

        public void Dispose()
        {
            var _ = Shutdown();
        }
    }
}
