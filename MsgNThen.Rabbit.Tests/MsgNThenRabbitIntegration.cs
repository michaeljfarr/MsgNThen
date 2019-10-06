using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace MsgNThen.Rabbit.Tests
{
    public class MsgNThenRabbitIntegration : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private const string ExchangeName = "TestEx";
        private const string QueueName = "TestQ";
        private readonly BlockMessageHandler _messageHandler;
        private readonly IRabbitMqListener _listener;
        private readonly IMessagePublisher _publisher;
        private readonly IConnection _rabbit;

        public MsgNThenRabbitIntegration(ITestOutputHelper output)
        {
            _output = output;
            var serviceCollection = CreateServiceCollection();
            serviceCollection.AddSingleton<IMessageHandler, BlockMessageHandler>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _messageHandler = (BlockMessageHandler)serviceProvider.GetRequiredService<IMessageHandler>();
            _listener = serviceProvider.GetRequiredService<IRabbitMqListener>();
            _publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            _rabbit = serviceProvider.GetRequiredService<IConnection>();
            _output.WriteLine("Binding");

            _publisher.BindDirectQueue(ExchangeName, QueueName);
        }

        [Theory]
        [InlineData(5)]
        public void TestMaxTasks(ushort maxTasks)
        {
            _messageHandler.Block = true;
            _output.WriteLine("Sending warmup messages");
            SendMessages(_publisher, ExchangeName, maxTasks*2);

            _output.WriteLine("Listening");
            var isOpen = _listener.Listen(QueueName, maxTasks);
            isOpen.Should().Be(true);

            for (int i = 0; i < 10 && _listener.NumTasks < maxTasks; i++)
            {
                Thread.Sleep(100);
            }
            _listener.NumTasks.Should().Be(maxTasks);
            _output.WriteLine("Waiting for thread count to grow ...");
            Thread.Sleep(1000);
            //checking that the thread count didn't grow
            _listener.NumTasks.Should().Be(maxTasks);
        }

        [Fact]
        public void TestStopAndStartListening()
        {
            _messageHandler.Block = false;
            _listener.Remove(QueueName);
            //wait for all tasks to finish
            for (int i = 0; i < 10 && _listener.NumTasks > 0; i++)
            {
                Thread.Sleep(10);
            }
            _listener.NumTasks.Should().Be(0);
            
            var numHandled = _messageHandler.Count;
            //send some messages
            SendMessages(_publisher, ExchangeName, 10);
            //see that no tasks get created while waiting for 1 second
            for (int i = 0; i < 10 && _listener.NumTasks == 0; i++)
            {
                Thread.Sleep(100);
            }
            //see that no messages where handled
            _messageHandler.Count.Should().Be(numHandled);
            _listener.Listen(QueueName, 10);
            Thread.Sleep(100);
            _messageHandler.Count.Should().BeGreaterThan(numHandled);
            //drain the messages
            for (int i = 0; i < 100 && _messageHandler.Count > numHandled; i++)
            {
                Thread.Sleep(100);
            }
        }


        [Theory]
        [InlineData(20000, 2000, 10)]

        public (double perSecWriteThroughput, double perSecReadThroughput) SmallMessageThroughput(int numMessages, int minThroughput, ushort maxTasks)
        {
            _output.WriteLine("Listening");
            var isOpen = _listener.Listen(QueueName, maxTasks);
            isOpen.Should().Be(true);
            _output.WriteLine($"Sending {numMessages} tiny messages");

            var writerSw = Stopwatch.StartNew();
            SendMessages(_publisher, ExchangeName, numMessages);
            writerSw.Stop();
            var startNum = _messageHandler.Count;
            _messageHandler.Block = false;
            var readerSw = Stopwatch.StartNew();

            //wait for max 20 seconds
            for (int i = 0; i<20 && (_messageHandler.Count < (numMessages * 0.75)); i++)
            {
                Thread.Sleep(1000);
            }

            _messageHandler.Count.Should().BeGreaterOrEqualTo((int)(numMessages * 0.75));
            var endNum = _messageHandler.Count;
            readerSw.Stop();
            var perSecWriteThroughput = (numMessages) / writerSw.Elapsed.TotalSeconds;
            var perSecReadThroughput = (endNum - startNum) / readerSw.Elapsed.TotalSeconds;
            perSecWriteThroughput.Should().BeGreaterOrEqualTo(minThroughput);
            perSecReadThroughput.Should().BeGreaterOrEqualTo(minThroughput);
            _output.WriteLine($"Done Write:{perSecWriteThroughput:F1}/s, Read:{perSecReadThroughput:F1}");
            return (perSecWriteThroughput, perSecReadThroughput);
        }


        private static void SendMessages(IMessagePublisher publisher, string exchangeName, int numMessages)
        {
            for (int i = 0; i < numMessages; i++)
            {
                publisher.PublishSingle(new SimpleMessage()
                {
                    Body = new byte[1] { (byte)'a' },
                    Exchange = exchangeName,
                    RoutingKey = null,
                    Properties = new MessageProperties()
                    {
                    }
                }, null);
            }
        }

        public static ServiceCollection CreateServiceCollection()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("settings.json");

            var configuration = builder.Build();

            var services = new ServiceCollection();
            services.ConfigureRabbit(configuration);
            services.AddRabbit();
            services.AddLogging();
            return services;
        }

        public void Dispose()
        {
            _rabbit.Close(1000);
        }

    }
}
