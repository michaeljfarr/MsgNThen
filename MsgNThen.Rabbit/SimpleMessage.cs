using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    public class SimpleMessage
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public IBasicProperties Properties { get; set; }
        public byte[] Body { get; set; }
    }
}