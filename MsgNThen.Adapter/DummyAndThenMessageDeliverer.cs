using MsgNThen.Interfaces;
using MsgNThen.Redis.NThen;

namespace MsgNThen.Adapter
{
    class DummyAndThenMessageDeliverer : IAndThenMessageDeliverer
    {
        public void Deliver(SimpleMessage andThen)
        {
            //todo: make SimpleMessage more generic, use Uris to:
            // + target SQS, RabbitMQ, Redis, Http endpoints
            // + will use dictionary to make scheme handlers

            
        }
    }
}