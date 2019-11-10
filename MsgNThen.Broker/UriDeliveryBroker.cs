using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MsgNThen.Interfaces;

namespace MsgNThen.Broker
{
    class UriDeliveryBroker : IUriDeliveryBroker
    {
        private readonly IDictionary<string, IUriDeliveryScheme> _deliverySchemes;

        public UriDeliveryBroker(IEnumerable<IUriDeliveryScheme> deliverySchemes)
        {
            _deliverySchemes = deliverySchemes.ToDictionary(a=>a.Scheme);
        }

        public Task Deliver(Uri destination, MsgNThenMessage message)
        {
            if (_deliverySchemes.TryGetValue(destination.Scheme, out var scheme))
            {
                return scheme.Deliver(destination, message);
            }
            else
            {
                throw new Exception($"Failed to find ({nameof(IUriDeliveryScheme)}) for {destination}");
            }
        }
    }
}