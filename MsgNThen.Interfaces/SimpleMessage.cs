namespace MsgNThen.Interfaces
{
    public class SimpleMessage
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public MessageProperties Properties { get; set; }
        public byte[] Body { get; set; }
    }
}