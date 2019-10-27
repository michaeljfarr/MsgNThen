using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Redis.NThen;


namespace MsgNThen.Adapter
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMsgNThenServer(this IServiceCollection services)
        {
            AddMsgNThenAdapter(services);
            services.AddSingleton<IServer, MsgNThenServer>();
        }

        public static void AddMsgNThenAdapter(this IServiceCollection services)
        {
            services.AddSingleton<IMessageHandler, MsgNThenApdapterFactory>();
            services.AddSingleton<IMsgNThenApdapterFactory>(a =>
                (IMsgNThenApdapterFactory) a.GetRequiredService<IMessageHandler>());
        }

        public static void AddDummyAndThenMessageDeliverer(this IServiceCollection services)
        {
            services.AddSingleton<IAndThenMessageDeliverer, DummyAndThenMessageDeliverer>();
        }

    }
}