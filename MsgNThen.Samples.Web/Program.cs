using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MsgNThen.Adapter;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Samples.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //create a standard HostBuilder, but with a MsgNThen Server instead of Kestrel
            var hostBuilder = CreateHostBuilder(args);
            var build = hostBuilder.Build();
            var runTask = build.RunAsync();

            await SendDummyMessage(build);

            await runTask;
            //uncomment if you want a way to bin the server and exit the process.
            //await build.StopAsync();
            //((IDisposable)build.Services).Dispose();
        }

        private static async Task SendDummyMessage(IHost build)
        {
            var msgNThenHandler = build.Services.GetRequiredService<IMessageHandler>();
            
            await Task.Delay(500);
            
            IHandledMessage message = new HandledMessageWrapper(new BasicDeliverEventArgs(
                "consumer", 1, false, "ex", "exampleroute", null, new byte[0]));
            await msgNThenHandler.HandleMessageTask(message);
        }


        //Using ConfigureWebHost instead of ConfigureWebHostDefaults still adds GenericWebHostService, but without
        //invoking Microsoft.AspNetCore.WebHost.ConfigureWebDefaults which excludes:
        // - UseStaticWebAssets, UseKestrel, UseIIS and UseIISIntegration
        // - UseKestrel creates a KestrelServer:IServer to handle the HTTP protocol, but we use MsgNThenServer just to direct messages from Rabbit.

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).ConfigureServices(services =>
                {
                    services.AddMsgNThenServer();
                });

    }
}
