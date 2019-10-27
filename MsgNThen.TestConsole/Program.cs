using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Adapter.Tests;
using MsgNThen.Rabbit;
using MsgNThen.Rabbit.Tests;
using MsgNThen.Redis;

namespace MsgNThen.TestConsole
{
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
            var adapterTest = new BasicIntegrationTests();
            adapterTest.SendThreeAndWaitForFourth();
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
