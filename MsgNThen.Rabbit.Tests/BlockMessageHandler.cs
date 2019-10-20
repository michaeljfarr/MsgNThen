using System;
using System.Threading;
using System.Threading.Tasks;
using MsgNThen.Interfaces;

namespace MsgNThen.Rabbit.Tests
{
    
    public class BlockNThenMessageHandler : IMessageHandler
    {
        private readonly IMessageGroupHandler _messageGroupHandler;
        public AutoResetEvent Block { get; set; } = new AutoResetEvent(false);
        public int Received => _received;
        private int _received;
        public int Handled => _handled;
        private int _handled;

        public BlockNThenMessageHandler(IMessageGroupHandler messageGroupHandler)
        {
            _messageGroupHandler = messageGroupHandler;
        }


        public Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            Interlocked.Increment(ref _received);
            while (!Block.WaitOne(500))
            {
            }
            Interlocked.Increment(ref _handled);

            if (Guid.TryParse(message.Properties.MessageId, out var messageId) &&
                message.Properties.Headers.TryGetValue("MessageGroupId", out var groupIdObj))
            {
                var groupId = new Guid((byte[]) groupIdObj);
                _messageGroupHandler.MessageHandled(groupId, messageId);
            }
            else
            {
            }

            return Task.FromResult(AckResult.Ack);
        }
    }

    public class BlockMessageHandler : IMessageHandler
    {
        private int _count;
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