using MsgNThen.Interfaces;
using MsgNThen.Redis.NThen;

namespace MsgNThen.Adapter
{
    class DummyAndThenMessageDeliverer : IAndThenMessageDeliverer
    {
        public void Deliver(SimpleMessage andThen)
        {
            
        }
    }
}