using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsgNThen.Adapter;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Samples.Web
{
    //public class MsgNThenContextFactory : IHttpContextFactory
    //{
    //    private DefaultHttpContextFactory _df;

    //    public MsgNThenContextFactory(IServiceProvider serviceProvider)
    //    {
    //        _df = new DefaultHttpContextFactory(serviceProvider);
    //    }
    //    HttpContext IHttpContextFactory.Create(IFeatureCollection featureCollection)
    //    {
    //        return _df.Create(featureCollection);
    //    }

    //    public void Dispose(HttpContext httpContext)
    //    {
    //        _df.Dispose(httpContext);
    //    }
    //}

    
    //public class MsgNThenService : IHostedService
    //{
    //    public Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.Delay(100);
    //    }

    //    public Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.Delay(100);
    //    }
    //}
    public class Program
    {
        public static async Task Main(string[] args)
        {
            
            //We should be an IHostedService
            var hostBuilder = CreateHostBuilder(args);
            var build = hostBuilder.Build();

            var msgNThenHandler = build.Services.GetRequiredService<IMessageHandler>();
            var startTask = build.StartAsync();
            await Task.Delay(500);

            IHandledMessage message = new HandledMessageWrapper(new BasicDeliverEventArgs(
                "consumer", 1, false, "ex", "rout", new BasicProperties(), new byte[0]));
            await msgNThenHandler.HandleMessageTask(message);

            await startTask;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            MsgNThenHost.CreateDefaultBuilder(args)
                .ConfigureMsgNThenHost(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static IHostBuilder CreateHostBuilderStandard(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    //services.AddHostedService<MsgNThenService>();
                });

    }

    public static class MsgNThenHost
    {

        public static IHostBuilder CreateDefaultBuilder(string[] args)
        {
            var builder = new HostBuilder();

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            }).
            UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            });

            ConfigureWebDefaults(builder);

            return builder;
        }

        public static IHostBuilder ConfigureMsgNThenHost(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
            //This adds GenericWebHostService, but without Microsoft.AspNetCore.WebHost.ConfigureWebDefaults which excludes:
            // - UseStaticWebAssets, UseKestrel, UseIIS and UseIISIntegration
            // - UseKestrel creates a KestrelServer:IServer to handle the HTTP protocol, but we use MsgNThenServer just to direct messages from Rabbit.
            builder.ConfigureServices((hostingContext, services) =>
            {
                //services.AddSingleton<IHttpContextFactory, MsgNThenContextFactory>();
            });
            return builder.ConfigureWebHost(webHostBuilder =>
            {
                configure(webHostBuilder);
            });
        }
        internal static void ConfigureWebDefaults(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((ctx, cb) =>
            {
            });
            builder.ConfigureServices((hostingContext, services) =>
            {
                services.AddRouting();
                services.AddMsgNThenServer();
            });
        }
    }
}
