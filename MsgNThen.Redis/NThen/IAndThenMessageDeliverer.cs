using MsgNThen.Interfaces;

namespace MsgNThen.Redis.NThen
{
    public interface IAndThenMessageDeliverer
    {
        void Deliver(SimpleMessage andThen);
    }
}