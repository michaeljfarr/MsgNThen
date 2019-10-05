using System;
using System.Collections.Generic;
using MsgNThen.Interfaces;
using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    public class BasicPropertiesWrapper : IHandledMessageProperties
    {
        private readonly IBasicProperties _basicProperties;
        private readonly DateTime _timestamp;

        public BasicPropertiesWrapper(IBasicProperties basicProperties)
        {
            _basicProperties = basicProperties;
            _timestamp = DateTime.FromFileTimeUtc(basicProperties.Timestamp.UnixTime);
        }

        public string AppId => _basicProperties.AppId;

        public string ContentEncoding => _basicProperties.ContentEncoding;

        public string ContentType => _basicProperties.ContentType;

        public string CorrelationId => _basicProperties.CorrelationId;

        public byte DeliveryMode => _basicProperties.DeliveryMode;

        public string Expiration => _basicProperties.Expiration;

        public IDictionary<string, object> Headers => _basicProperties.Headers;

        public string MessageId => _basicProperties.MessageId;

        public byte Priority => _basicProperties.Priority;

        public string ReplyTo => _basicProperties.ReplyTo;

        public DateTime Timestamp => _timestamp;

        public string Type => _basicProperties.Type;

        public string UserId => _basicProperties.UserId;
    }
}