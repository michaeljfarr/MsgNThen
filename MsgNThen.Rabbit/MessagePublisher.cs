using System;
using System.Collections.Generic;
using MsgNThen.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Rabbit
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IConnection _connection;
        private IModel _channel;

        public MessagePublisher(IConnection connection)
        {
            _connection = connection;
            _channel = _connection.CreateModel();
            //            ch.Close(Constants.ReplySuccess, "Closing the channel");

        }

        public void BindDirectQueue(string exchangeName, string queueName)
        {
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(queueName, false, false, false, null);
            _channel.QueueBind(queueName, exchangeName, "", null);
        }

        public void PublishSingle(SimpleMessage message, SimpleMessage andThen)
        {
            var groupId = Guid.NewGuid();

            var basicProperties = ToBasicProperties(_channel.CreateBasicProperties(), message, groupId);

            _channel.BasicPublish(message.Exchange, message.RoutingKey ?? "", true, basicProperties, message.Body);
        }

        public void PublishBatch(IEnumerable<SimpleMessage> messages, SimpleMessage andThen, int mode)
        {
            var groupId = Guid.NewGuid();
            var batch = _channel.CreateBasicPublishBatch();

            int messageSentCounter = 0;
            foreach (var message in messages)
            {
                var basicProperties = ToBasicProperties(_channel.CreateBasicProperties(), message, groupId);
                batch.Add(message.Exchange, message.RoutingKey, true, basicProperties, message.Body);
                messageSentCounter++;
            }
            //record event in redis
            //mode 1: client sends andThen after every message so a separate handler can check the message count
            //mode 2: server sends andThen at end to separate handler that polls redis 
            //note: polling is required anyway if you want a timeout on the message group
            //mode 2 can be more efficient if there is a large number of messages
            //mode 1 means the clients can be unaware of redis

            batch.Publish();
            if (andThen != null)
            {
                var andThenProperties = ToBasicProperties(_channel.CreateBasicProperties(), andThen, groupId);


                andThen.Properties.Headers["MessagesSent"] = messageSentCounter;
                //mark set as sent in redis and attach message to send when complete
                if (mode == 1)
                {
                    //client will send andThen message after it handles each message.
                    //redis.CompleteBatch(groupId, andThen)
                    _channel.BasicPublish(andThen.Exchange, andThen.RoutingKey, true, andThenProperties, andThen.Body);
                }
                else if (mode == 2)
                {
                    //send the andThen message to the service that will now poll redis
                    _channel.BasicPublish(andThen.Exchange, andThen.RoutingKey, true, andThenProperties, andThen.Body);
                }
            }
        }

        private static IBasicProperties ToBasicProperties(IBasicProperties basicProperties, SimpleMessage message,
            Guid groupId)
        {
            if (!string.IsNullOrWhiteSpace(message.Properties.CorrelationId))
            {
                basicProperties.CorrelationId = message.Properties.CorrelationId;
            }
            if (!string.IsNullOrWhiteSpace(message.Properties.MessageId))
            {
                basicProperties.MessageId = message.Properties.MessageId;
            }
            if (!string.IsNullOrWhiteSpace(message.Properties.CorrelationId))
            {
                basicProperties.CorrelationId = message.Properties.CorrelationId;
            }

            if (basicProperties.Headers == null)
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }
            basicProperties.Headers["MessageGroupId"] = groupId.ToByteArray();
            return basicProperties;
        }
    }
}