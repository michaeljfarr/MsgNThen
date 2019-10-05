using System;

namespace MsgNThen.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        static void RunRabbitConsole(string[] args)
        {
            //start test rabbitmq receiver
            //start AndThen handler
            //send message group
            //assert that messages are received by test receiver
            //assert that AndThen activity occurs once last message is processed

            //theories to test: 
            // if our batch size is n, our max task count is also n.
            // we can start and stop listening from a queue??
        }
    }
}
