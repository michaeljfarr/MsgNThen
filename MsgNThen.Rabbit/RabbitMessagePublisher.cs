using System.Collections.Generic;
using MsgNThen.Interfaces;
using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    public class RabbitMessagePublisher : IMessagePublisher
    {
        private readonly IModel _channel;

        public RabbitMessagePublisher(IConnection connection)
        {
            _channel = connection.CreateModel();
        }

        public void BindDirectQueue(string exchangeName, string queueName)
        {
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(queueName, false, false, false, null);
            _channel.QueueBind(queueName, exchangeName, "", null);
        }

        public void Publish(IDictionary<string, object> extraHeaders, SimpleMessage message)
        {
        
            var basicProperties = ToBasicProperties(_channel.CreateBasicProperties(), message);
            ApplyHeaders(extraHeaders, basicProperties);
            _channel.BasicPublish(message.Exchange, message.RoutingKey, true, basicProperties, message.Body);
        }

        private static void ApplyHeaders(IDictionary<string, object> extraHeaders, IBasicProperties basicProperties)
        {
            foreach (var extraHeader in extraHeaders ?? new Dictionary<string, object>())
            {
                basicProperties.Headers[extraHeader.Key] = extraHeader.Value;
            }
        }

        public int PublishBatch(IDictionary<string, object> extraHeaders, IEnumerable<SimpleMessage> messages,
            AndThenDeliveryMode mode)
        {
            var batch = _channel.CreateBasicPublishBatch();
            int messageSentCounter = 0;
            foreach (var message in messages)
            {
                var basicProperties = ToBasicProperties(_channel.CreateBasicProperties(), message);
                ApplyHeaders(extraHeaders, basicProperties);
                batch.Add(message.Exchange, message.RoutingKey ?? "", true, basicProperties, message.Body);
                messageSentCounter++;
            }
            batch.Publish();
            return messageSentCounter;
        }

        private static IBasicProperties ToBasicProperties(IBasicProperties basicProperties, SimpleMessage message)
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

            return basicProperties;
        }
    }
}