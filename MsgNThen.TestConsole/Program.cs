﻿using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using MsgNThen.Rabbit.Tests;
using MsgNThen.Redis;
using MsgNThen.Redis.NThen;
using NSubstitute;
using RabbitMQ.Client;

namespace MsgNThen.TestConsole
{
    public class RabbitAndThenMessageDeliverer : IAndThenMessageDeliverer
    {
        private readonly IMessagePublisher _messagePublisher;
        private static readonly Dictionary<string, object> ExtraHeaders = new Dictionary<string, object>();

        public RabbitAndThenMessageDeliverer(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }

        public void Deliver(SimpleMessage andThen)
        {
            _messagePublisher.Publish(ExtraHeaders, andThen);
        }
    }

    class Program
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

        static void Main(string[] args)
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
            messageHandler.Block.Set();
            WaitTill(messageHandler, 3).Should().BeTrue();
            messageHandler.Block.Set();
            WaitTill(messageHandler, 4).Should().BeTrue();
            messageHandler.Handled.Should().Be(4);
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

        private static void RunRabbitIntegrationTests(string[] args)
        {
            var maxTasks = GetUShort(args, 0, 5);
            using (var rabbitTester = new MsgNThenRabbitIntegration(new ConsoleOutput()))
            {
                //start test rabbitmq receiver
                //start AndThen handler
                //send message group
                //assert that messages are received by test receiver
                //assert that AndThen activity occurs once last message is processed

                //theories to test: 
                // (done) if our batch size is n, our max task count is also n.
                rabbitTester.TestMaxTasks(maxTasks);

                rabbitTester.SmallMessageThroughput(20000, 2000, maxTasks);
                // (done) we can send/recieve 6000+ messages a second on single machine via a single queue (10 Tasks)
                //Here are 2 examples from my Dell XPS 9550 with rabbit running on WSL ubuntu:
                //maxTasks=20 => Write:31749.0/s, Read:6660.6  (CPU was at about 70%, up from a baseline of 15% caused by spotify and chrome)
                //maxTasks=1 => Write:30160.7/s, Read:2153.7

                // (done) we can start and stop listening from a queue
                rabbitTester.TestStopAndStartListening();
            }
        }

        private static ushort GetUShort(string[] args, int index, ushort def)
        {
            if (args.Length >= index)
            {
                if (ushort.TryParse(args[index], out ushort res))
                {
                    return res;
                }
            }

            return def;
        }


    }
}
