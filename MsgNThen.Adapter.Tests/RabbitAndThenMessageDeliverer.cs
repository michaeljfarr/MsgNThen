using System.Collections.Generic;
using MsgNThen.Interfaces;
using MsgNThen.Redis.NThen;

namespace MsgNThen.Adapter.Tests
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
}