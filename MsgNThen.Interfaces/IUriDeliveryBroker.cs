using System;
using System.Threading.Tasks;

namespace MsgNThen.Interfaces
{
    public interface IUriDeliveryBroker
    {
        Task Deliver(Uri destination, MsgNThenMessage message);
    }

    public interface IUriDeliveryScheme
    {
        string Scheme { get; }
        Task Deliver(Uri destination, MsgNThenMessage message);
    }
}