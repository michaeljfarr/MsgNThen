using System;
using Xunit.Abstractions;

namespace MsgNThen.TestConsole
{
    class ConsoleOutput : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(string.Format(format, args));
        }
    }
}