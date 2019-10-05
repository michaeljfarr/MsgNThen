using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    class RabbitConnectionFactory : IRabbitConnectionFactory
    {
        private readonly RabbitConfig _rabbitConfig;
        private readonly ConnectionFactory _connectionFactory;

        public RabbitConnectionFactory(IOptionsMonitor<RabbitConfig> rabbitConfig)
        {
            _rabbitConfig = rabbitConfig.CurrentValue;
            _connectionFactory = new ConnectionFactory
            {
                UserName = _rabbitConfig.UserName,
                Password = _rabbitConfig.Password,
                VirtualHost = _rabbitConfig.VirtualHost,
                Port = _rabbitConfig.Port
            };
        }

        public IConnection Create()
        {
            return _connectionFactory.CreateConnection(_rabbitConfig.HostNames);
        }
    }
}