using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    public interface IRabbitConnectionFactory
    {
        IConnection Create();
    }
}