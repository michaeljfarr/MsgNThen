using MsgNThen.Interfaces;

namespace MsgNThen.Redis.NThen
{
    public interface IMessageDeliverer
    {
        void Deliver(SimpleMessage andThen);
    }
}