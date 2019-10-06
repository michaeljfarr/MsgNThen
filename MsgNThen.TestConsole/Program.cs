using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using MsgNThen.Rabbit.Tests;
using RabbitMQ.Client;

namespace MsgNThen.TestConsole
{
    class Program
    {
        static void Main(string[] args)
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
                //maxTasks=20 => Write:31749.0/s, Read:6660.6
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
