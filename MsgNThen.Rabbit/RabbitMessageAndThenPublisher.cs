using System;
using System.Collections.Generic;
using System.Linq;
using MsgNThen.Interfaces;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Rabbit
{

    public class RabbitMessageAndThenPublisher : IMessageAndThenPublisher
    {
        private readonly IMessagePublisher _messagePublisher;
        private readonly IMessageGroupHandler _messageGroupHandler;

        public RabbitMessageAndThenPublisher(IMessagePublisher messagePublisher, IMessageGroupHandler messageGroupHandler)
        {
            _messagePublisher = messagePublisher;
            _messageGroupHandler = messageGroupHandler;
        }

        public void BindDirectQueue(string exchangeName, string queueName)
        {
            _messagePublisher.BindDirectQueue(exchangeName, queueName);
        }

        public void PublishSingle(SimpleMessage message, SimpleMessage andThen, AndThenDeliveryMode mode)
        {
            PublishBatch(new[] { message }, andThen, mode);
        }

        public void PublishBatch(IReadOnlyList<SimpleMessage> messages, SimpleMessage andThen, AndThenDeliveryMode mode)
        {
            var groupId = Guid.NewGuid();
            var headers = new Dictionary<string, object>
            {
                { "MessageGroupId", groupId.ToByteArray()},
                { "AndThenMode", mode.ToString() }
            };
            //initialize the Redis List and Hash 
            if (mode != AndThenDeliveryMode.None)
            {
                _messageGroupHandler.StartMessageGroup(groupId);
            }

            var messageIds = InitializeMessageIds(messages);

            //store message ids in the redis Redis List then
            //set the MsgCount, Handled, DateStamp and the AndThen message
            if (mode != AndThenDeliveryMode.None)
            {
                _messageGroupHandler.MessagesPrepared(groupId, messageIds);
                _messageGroupHandler.SetMessageGroupCount(groupId, messageIds.Count, andThen);
            }

            _messagePublisher.PublishBatch(headers, messages, mode);

            //finalize the redis structure by setting the Transmitted date stamp
            if (mode != AndThenDeliveryMode.None)
            {
                _messageGroupHandler.CompleteMessageGroupTransmission(groupId);
            }

            if (andThen?.Body == null && mode == AndThenDeliveryMode.FromLastClient)
            {
                throw new Exception($"The AndThen body must be specified in the {AndThenDeliveryMode.FromLastClient} mode.");
            }
            if (andThen == null)
            {
                andThen = new SimpleMessage()
                {
                    Exchange = "AndThen",
                    RoutingKey = messages.First().RoutingKey,
                    Body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(messageIds)
                };
            }
            else if (andThen?.Body == null)
            {
                andThen.Body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(messageIds);
            }

            headers["MessagesSent"] = messageIds.Count;
            switch (mode)
            {
                case AndThenDeliveryMode.FromLastClient:
                    //client will send the andThen message that it read from IMessageGroupHandler (redis).
                    break;
                case AndThenDeliveryMode.FromEventService:
                    //client will send an event to the eventService after it handles each message.
                    _messagePublisher.Publish(headers, andThen);
                    break;
                case AndThenDeliveryMode.FromPollingService:
                    //send the andThen message to the service that will now poll redis
                    _messagePublisher.Publish(headers, andThen);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static List<Guid> InitializeMessageIds(IReadOnlyList<SimpleMessage> messages)
        {
            var messageIds = new List<Guid>();
            foreach (var message in messages)
            {
                if (!string.IsNullOrWhiteSpace(message.Properties?.MessageId))
                {
                    if (Guid.TryParse(message.Properties.MessageId, out var messageId))
                    {
                        messageIds.Add(messageId);
                    }
                    else
                    {
                        throw new ApplicationException($"MessageIds was not a Guid: {message.Properties.MessageId}");
                    }
                }
                else
                {
                    if (message.Properties == null)
                    {
                        message.Properties = new MessageProperties();
                    }

                    var messageId = Guid.NewGuid();
                    message.Properties.MessageId = messageId.ToString();
                    messageIds.Add(messageId);
                }
            }

            return messageIds;
        }
    }
}