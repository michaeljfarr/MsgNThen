using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using RabbitMQ.Client;

namespace MsgNThen.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            RunRabbitConsole(args);
        }

        static void RunRabbitConsole(string[] args)
        {
            var serviceCollection = CreateServiceCollection();
            serviceCollection.AddSingleton<IMessageHandler, BlockMessageHandler>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var messageHandler = serviceProvider.GetRequiredService<IMessageHandler>();
            var listener = serviceProvider.GetRequiredService<IRabbitMqListener>();
            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            var rabbit = serviceProvider.GetRequiredService<IConnection>();
            var exchangeName = "TestEx";
            var queueName = "TestQ";
            Console.WriteLine("Binding");

            publisher.BindDirectQueue(exchangeName, queueName);
            Console.WriteLine("Sending warmup messages");
            SendMessages(publisher, exchangeName, 15);
            
            var maxTasks = GetUShort(args, 0, 5);
            Console.WriteLine("Listening");
            listener.Listen(queueName, maxTasks);
            
            for (int i = 0; i<10 && listener.NumTasks < maxTasks; i++)
            {
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
            var curNumTasks = listener.NumTasks;
            if (curNumTasks > maxTasks)
            {
                throw new ApplicationException($"Unexpected number of threads: {curNumTasks}");
            }
            

            var blocker = (BlockMessageHandler)messageHandler;
            blocker.Block = false;
            var numMessages = 20000;
            Console.WriteLine($"Sending {numMessages} tiny messages");
            var startNum = blocker.Count;
            var sw = Stopwatch.StartNew();
            SendMessages(publisher, exchangeName, numMessages);
            while (blocker.Count < (numMessages * 0.75))
            {
                Thread.Sleep(1000);
            }

            var endNum = blocker.Count;
            sw.Stop();
            var perSecThroughput = (endNum - startNum) / sw.Elapsed.TotalSeconds;
            //start test rabbitmq receiver
            //start AndThen handler
            //send message group
            //assert that messages are received by test receiver
            //assert that AndThen activity occurs once last message is processed

            //theories to test: 
            // (done) if our batch size is n, our max task count is also n.
            // we can start and stop listening from a queue??
            // (done) we can send/recieve 6000+ messages a second on single machine via a single queue if we dont batch (10 Tasks)
            Console.WriteLine($"Done {perSecThroughput:F1}");
            rabbit.Close(1000);
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

        private static void SendMessages(IMessagePublisher publisher, string exchangeName, int numMessages)
        {
            for (int i = 0; i < numMessages; i++)
            {
                publisher.PublishSingle(new SimpleMessage()
                {
                    Body = new byte[1] {(byte) 'a'},
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

    }
}
