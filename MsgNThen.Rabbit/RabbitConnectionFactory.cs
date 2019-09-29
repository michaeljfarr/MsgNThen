using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    class RabbitConnectionFactory : IRabbitConnectionFactory
    {
        private readonly RabbitConfig _rabbitConfig;
        private readonly ConnectionFactory _connectionFactory;

        public RabbitConnectionFactory(RabbitConfig rabbitConfig)
        {
            _rabbitConfig = rabbitConfig;
            _connectionFactory = new ConnectionFactory
            {
                UserName = rabbitConfig.UserName,
                Password = rabbitConfig.Password,
                VirtualHost = rabbitConfig.VirtualHost,
                Port = rabbitConfig.Port
            };
        }

        public IConnection Create()
        {
            return _connectionFactory.CreateConnection(_rabbitConfig.HostNames);
        }

        public static IServiceCollection ConfigureRabbit(IServiceCollection services,  IConfiguration configuration)
        {
            return services.Configure<RabbitConfig>(configuration);
        }
    }
}