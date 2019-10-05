using MsgNThen.Interfaces;

namespace MsgNThen.Redis
{
    public interface IMessageDeliverer
    {
        void Deliver(SimpleMessage andThen);
    }
}