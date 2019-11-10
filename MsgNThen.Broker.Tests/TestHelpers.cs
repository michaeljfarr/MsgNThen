using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using Xunit;

namespace MsgNThen.Broker.Tests
{
    public class TestHelpers
    {
        

        public static ServiceProvider CreateServiceProvider()
        {
            var services = CreateServiceCollection();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        public static ServiceCollection CreateServiceCollection()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("settings.json", true).AddUserSecrets<TestHelpers>();

            var configuration = builder.Build();

            var services = new ServiceCollection();
            services.ConfigureAwsCredentials(configuration);
            services.AddBrokers();
            services.AddMsgnThenRabbit();
            services.AddSingleton<IMessageHandler, OneMessageHandler>();
            services.AddLogging();
            return services;
        }
    }
}