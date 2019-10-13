using System;
using System.Collections.Generic;

namespace MsgNThen.Interfaces
{
    //record event in redis
    //mode 1: client sends andThen after every message so a separate handler can check the message count
    //mode 2: server sends andThen at end to separate handler that polls redis 
    //note: polling is required anyway if you want a timeout on the message group
    //mode 2 can be more efficient if there is a large number of messages
    //mode 1 means the clients can be unaware of redis

    public enum AndThenDeliveryMode
    {
        /// <summary>
        /// The andThen messages are not sent, the whole andThen subsystem is skipped for this mode.
        /// </summary>
        None,
        /// <summary>
        /// Each client interacts with Redis, and will send the andThen message once the batch is empty.  
        /// Currently the andThen message can only be delivered in an at-least-once configuration.
        /// </summary>
        /// <remarks>
        /// This is probably the best general purpose solution for this, but it relies on every client
        /// having equal access to redis and rabbit.
        /// </remarks>
        FromLastClient,
        /// <summary>
        /// The client will send an event after handling each message, so a central service can send the andThen.
        /// In this mode, the client can be unaware of redis.
        /// </summary>
        FromEventService,
        /// <summary>
        /// The client will update redis after handling each message, but it will rely on a central service to send the andThen.
        /// This is necessary when clients can't access the andThen queue or when the andThen must be sent at-most-once.
        /// </summary>
        FromPollingService
    }
    public interface IMessageAndThenPublisher
    {
        void BindDirectQueue(string exchangeName, string queueName);
        void PublishSingle(SimpleMessage message, SimpleMessage andThen, AndThenDeliveryMode mode);
        void PublishBatch(IReadOnlyList<SimpleMessage> messages, SimpleMessage andThen, AndThenDeliveryMode mode);
    }

    public interface IMessagePublisher
    {
        void BindDirectQueue(string exchangeName, string queueName);
        void Publish(IDictionary<string, object> extraHeaders, SimpleMessage message);
        int PublishBatch(IDictionary<string, object> extraHeaders, IEnumerable<SimpleMessage> messages,
            AndThenDeliveryMode mode);
    }
}
