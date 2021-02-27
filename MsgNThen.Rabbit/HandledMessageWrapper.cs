using System;
using MsgNThen.Interfaces;
using RabbitMQ.Client.Events;

namespace MsgNThen.Rabbit
{
    public class HandledMessageWrapper : IHandledMessage
    {
        private readonly BasicDeliverEventArgs _deliverEvent;
        private readonly BasicPropertiesWrapper _properties;

        public HandledMessageWrapper(BasicDeliverEventArgs deliverEvent)
        {
            _deliverEvent = deliverEvent;
            _properties = new BasicPropertiesWrapper(deliverEvent.BasicProperties);
        }

        public ReadOnlyMemory<byte> Body => _deliverEvent.Body;

        public string ConsumerTag => _deliverEvent.ConsumerTag;

        public ulong DeliveryTag => _deliverEvent.DeliveryTag;

        public string Exchange => _deliverEvent.Exchange;

        public bool Redelivered => _deliverEvent.Redelivered;

        public string RoutingKey => _deliverEvent.RoutingKey;

        public IHandledMessageProperties Properties => _properties;
    }
}