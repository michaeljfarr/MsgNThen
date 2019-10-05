using System.Threading;
using System.Threading.Tasks;
using MsgNThen.Interfaces;

namespace MsgNThen.TestConsole
{
    public class BlockMessageHandler : IMessageHandler
    {
        public int _count;
        public bool Block { get; set; } = true;
        public int Count => _count;

        public async Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            Interlocked.Increment(ref _count);
            while (Block)
            {
                await Task.Delay(500);
            }

            return AckResult.Ack;
        }
    }
}