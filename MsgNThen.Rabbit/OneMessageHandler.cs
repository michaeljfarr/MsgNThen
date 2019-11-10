using System;
using System.Threading;
using System.Threading.Tasks;
using MsgNThen.Interfaces;

namespace MsgNThen.Rabbit
{
    /// <summary>
    /// This is just for unit tests, it probably has threading and performance issues.
    /// </summary>
    public class OneMessageHandler : IMessageHandler
    {
        private IHandledMessage _message = null;
        private readonly AutoResetEvent _messagePopped = new AutoResetEvent(false);

        public IHandledMessage PopMessage()
        {
            lock (_messagePopped)
            {
                if (_message == null)
                {
                    return null;
                }

                _messagePopped.Set();
                return _message;
            }
        }

        private int _count;
        public int Count => _count;

        public Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            Interlocked.Increment(ref _count);
            while (!TryAssignMessage(message))
            {
                _messagePopped.WaitOne(TimeSpan.FromMilliseconds(500));
            }

            return Task.FromResult(AckResult.Ack);
        }

        private bool TryAssignMessage(IHandledMessage message)
        {
            lock (_messagePopped)
            {
                if (_message != null)
                {
                    return false;
                }

                _message = message;
                return true;
            }
        }
    }
}