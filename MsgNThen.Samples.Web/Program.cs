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
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MsgNThen.Samples.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);
            var build = hostBuilder.Build();
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IHttpRequestFeature>((IHttpRequestFeature)new HttpRequestFeature());
            featureCollection.Set<IHttpResponseFeature>((IHttpResponseFeature)new HttpResponseFeature());

            var applicationBuilder = build.Services.GetRequiredService<IApplicationBuilderFactory>().CreateBuilder(featureCollection);
            var contextFactory = build.Services.GetRequiredService<IHttpContextFactory>();
            
            Startup.ConfigureStatic(applicationBuilder);
            var testDelegate = applicationBuilder.Build();

            var context = contextFactory.Create(featureCollection);
            context.Request.Path = "/weatherforecast";
            var bytes = new byte[0];
            context.Request.Body = new MemoryStream(bytes);
            context.User = ClaimsPrincipal.Current;
            context.Request.ContentLength = null;
            context.Request.Host = HostString.FromUriComponent("localhost");
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();
            //context.Request.PathBase = PathString.FromUriComponent(context.Request.Path);
            //context.Request.
            //var initator = new MsgnThenInitiator(logger, testDelegate);
            await testDelegate.Invoke(context);
            while (true)
            {
                Thread.Sleep(500);
            }

            //build.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            MsgNThenHost.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
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

        public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
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
            });
        }
    }
}
