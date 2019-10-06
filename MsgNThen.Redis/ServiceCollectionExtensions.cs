using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MsgNThen.Redis
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisOptions = ConfigurationOptions.Parse(configuration.RequireConfig("RedisConnection"));
            //redisOptions.Ssl = false;
            redisOptions.CertificateValidation += RedisOptions_CertificateValidation;
            services.AddSingleton(redisOptions);
        }

        private static bool RedisOptions_CertificateValidation(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static void AddRedisFactory(this IServiceCollection services)
        {
            services.AddSingleton(sp=>ConnectionMultiplexer.Connect(sp.GetRequiredService<ConfigurationOptions>()));
        }

        public static void AddRedisMonitor(this IServiceCollection services)
        {
            services.AddSingleton<RedisMonitor>();
        }

        public static void AddRedisPipework(this IServiceCollection services)
        {
            services.AddSingleton<IRedisTaskFunnel, RedisTaskFunnel>();
            services.AddSingleton<ITaskReader>(sp=> new AtLeastOnceTaskReader(sp.GetRequiredService<ILogger<AtLeastOnceTaskReader>>(), sp.GetRequiredService<IRedisTaskFunnel>(), sp.GetRequiredService<Dictionary<string, ITaskExecutor>>(), 5));
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