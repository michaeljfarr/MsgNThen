using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MsgNThen.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Rabbit
{
    public class RabbitMessagePublisher : IMessagePublisher, IUriDeliveryScheme
    {
        private readonly IModel _channel;

        public RabbitMessagePublisher(IConnection connection)
        {
            _channel = connection.CreateModel();
        }

        public void BindDirectQueue(string exchangeName, string queueName, string routingKey)
        {
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(queueName, false, false, false, null);
            _channel.QueueBind(queueName, exchangeName, routingKey, null);
        }

        public void Publish(IDictionary<string, object> extraHeaders, SimpleMessage message)
        {
        
            var basicProperties = ToBasicProperties(_channel.CreateBasicProperties(), message);
            ApplyHeaders(extraHeaders, basicProperties);
            _channel.BasicPublish(message.Exchange, message.RoutingKey ?? "", true, basicProperties, message.Body);
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
            if (!string.IsNullOrWhiteSpace(message.Properties?.CorrelationId))
            {
                basicProperties.CorrelationId = message.Properties.CorrelationId;
            }
            if (!string.IsNullOrWhiteSpace(message.Properties?.MessageId))
            {
                basicProperties.MessageId = message.Properties.MessageId;
            }
            if (!string.IsNullOrWhiteSpace(message.Properties?.CorrelationId))
            {
                basicProperties.CorrelationId = message.Properties.CorrelationId;
            }

            if (basicProperties.Headers == null)
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }

            return basicProperties;
        }

        public string Scheme => "rabbit";

        public Task Deliver(Uri destination, MsgNThenMessage message)
        {
            //rabbitmq://<exchangename>/<routingKey>
            var exchange = destination.Host;
            var routingKey = Uri.UnescapeDataString(destination.PathAndQuery).TrimStart('/');
            var basicProperties = new BasicProperties()
            {
                Headers = new Dictionary<string, object>()
            };
            if (message.Headers != null)
            {
                var nonHeaders = new HashSet<string>()
                {
                    HeaderConstants.MessageId, HeaderConstants.AppId, HeaderConstants.ReplyTo, HeaderConstants.UserId,
                    HeaderConstants.CorrelationId
                };
                DoAssignment(message.Headers[HeaderConstants.MessageId], val => basicProperties.MessageId = val);
                DoAssignment(message.Headers[HeaderConstants.AppId], val => basicProperties.AppId = val);
                DoAssignment(message.Headers[HeaderConstants.ReplyTo], val => basicProperties.ReplyTo = val);
                DoAssignment(message.Headers[HeaderConstants.UserId], val => basicProperties.UserId = val);
                DoAssignment(message.Headers[HeaderConstants.CorrelationId], val => basicProperties.CorrelationId = val);
                //DoAssignment(message.Headers[HeaderConstants.DeliveryMode], val => basicProperties.DeliveryMode = val);
                foreach (var header in message.Headers)
                {
                    if (!nonHeaders.Contains(header.Key))
                    {
                        basicProperties.Headers[header.Key] = header.Value;
                    }
                }
            }

            var body = ((MemoryStream)message.Body).ToArray();
            _channel.BasicPublish(exchange, routingKey, true, basicProperties, body);
            return Task.CompletedTask;
        }

        private static void DoAssignment(string message, Action<string> basicProperties)
        {
            if (message != null)
            {
                basicProperties(message);
            }
        }
    }
}