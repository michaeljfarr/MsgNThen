using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using MsgNThen.Rabbit.Tests;
using MsgNThen.Redis;
using MsgNThen.Redis.NThen;
using RabbitMQ.Client;
using Xunit;

namespace MsgNThen.Adapter.Tests
{
    public class BasicIntegrationTests
    {

        public static ServiceCollection CreateServiceCollection()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("settings.json");

            var configuration = builder.Build();

            var services = new ServiceCollection();
            services.ConfigureRabbit(configuration);
            services.ConfigureRedis(configuration);
            services.AddRabbit();
            services.AddLogging();
            services.AddRedisFactory();
            services.AddRedisMonitor();
            services.AddRedisPipework();
            services.AddRedisNThenEventHandler();

            return services;
        }

        [Fact]
        public void SendThreeAndWaitForFourth()
        {
            var serviceCollection = CreateServiceCollection();
            serviceCollection.AddSingleton<IMessageHandler, BlockNThenMessageHandler>();
            serviceCollection.AddSingleton<IAndThenMessageDeliverer, RabbitAndThenMessageDeliverer>();
            //send 3 messages to A, B, and C queues
            //block all handlers, prove that the and-then hasn't been called
            //handle the first two messages - each time checking the and-then hasn't been called
            //handle the last message - check that the and then is called

            //send messages with mode=none, see that no items are created in redis and redis configuration is not required.
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var messageHandler = (BlockNThenMessageHandler)serviceProvider.GetRequiredService<IMessageHandler>();

            var andThenPublisher = serviceProvider.GetRequiredService<IMessageAndThenPublisher>();
            var rabbit = serviceProvider.GetRequiredService<IConnection>();
            //_output.WriteLine("Binding");

            andThenPublisher.BindDirectQueue("a_ex", "a");
            andThenPublisher.BindDirectQueue("b_ex", "b");
            andThenPublisher.BindDirectQueue("c_ex", "c");
            andThenPublisher.BindDirectQueue("d_ex", "d");

            var listener = serviceProvider.GetRequiredService<IRabbitMqListener>();
            listener.Listen("a", 2);
            listener.Listen("b", 2);
            listener.Listen("c", 2);
            listener.Listen("d", 2);

            andThenPublisher.PublishBatch(new[]
                {
                    new SimpleMessage(){Exchange = "a_ex", Body = Serialize("a_ex")},
                    new SimpleMessage(){Exchange = "b_ex", Body = Serialize("b_ex")},
                    new SimpleMessage(){Exchange = "c_ex", Body = Serialize("c_ex")},
                },
                new SimpleMessage() { Exchange = "d_ex", Body = Serialize("d_ex") },
                AndThenDeliveryMode.FromLastClient);
            messageHandler.Handled.Should().Be(0);
            WaitTill(messageHandler, 1).Should().BeFalse();
            messageHandler.Block.Set();
            messageHandler.Received.Should().Be(3);
            WaitTill(messageHandler, 1).Should().BeTrue();
            WaitTill(messageHandler, 2).Should().BeFalse();
            messageHandler.Block.Set();
            WaitTill(messageHandler, 2).Should().BeTrue();
            WaitTill(messageHandler, 3).Should().BeFalse();
            messageHandler.Received.Should().Be(3);
            messageHandler.Block.Set();
            WaitTill(messageHandler, 3).Should().BeTrue();
            messageHandler.Received.Should().Be(3);
            messageHandler.Block.Set();
            WaitTill(messageHandler, 4).Should().BeTrue();
            messageHandler.Handled.Should().Be(4);
            messageHandler.Received.Should().Be(4);
            serviceProvider.Dispose();
        }

        private static bool WaitTill(BlockNThenMessageHandler messageHandler, int handled)
        {
            for (int i = 0; i < 100 && messageHandler.Handled < handled; i++)
            {
                Thread.Sleep(10);
            }

            return messageHandler.Handled >= handled;
        }

        public static byte[] Serialize<TValue>(TValue obj)
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }
}
