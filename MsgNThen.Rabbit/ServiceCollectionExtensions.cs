using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using RabbitMQ.Client;

namespace MsgNThen.Rabbit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRabbit(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<RabbitConfig>(configuration.GetSection("Rabbit"));
        }

        public static IServiceCollection AddRabbit(this IServiceCollection services)
        {
            services.AddSingleton<IRabbitConnectionFactory, RabbitConnectionFactory>();
            services.AddSingleton<IConnection>(a=>a.GetRequiredService<IRabbitConnectionFactory>().Create());
            services.AddSingleton<IRabbitMqListener, RabbitMqListener>();
            services.AddSingleton<IMessagePublisher, RabbitMessagePublisher>();
            services.AddSingleton<IMessageAndThenPublisher, RabbitMessageAndThenPublisher>();
            
            return services;
        }


        private static string RequireConfig(this IConfiguration configuration, string key)
        {
            var val = configuration[key];
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ApplicationException($"Required config with key {key} had no value or wasn't present.");
            }

            return val;
        }
    }
}