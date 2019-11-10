using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;

namespace MsgNThen.Broker
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAwsCredentials(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsCredentialRoot>(configuration);
            return services;
        }

        public static IServiceCollection AddBrokers(this IServiceCollection services)
        {
            services.AddSingleton<IUriDeliveryBroker, UriDeliveryBroker>();
            services.AddSingleton<IUriDeliveryScheme, S3DeliveryScheme>();

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