using System.Collections.Generic;

namespace MsgNThen.Interfaces
{
    public interface IMessagePublisher
    {
        void BindDirectQueue(string exchangeName, string queueName);
        void PublishSingle(SimpleMessage message, SimpleMessage andThen);
        void PublishBatch(IEnumerable<SimpleMessage> messages, SimpleMessage andThen, int mode);

    }
}
