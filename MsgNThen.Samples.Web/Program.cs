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

    public class MsgNThenServer : IServer
    {
        public MsgNThenServer()
        {
            Features = new FeatureCollection();
            //MsgNThenRequestFeature Features.Set<IHttpRequestFeature>((IHttpRequestFeature)new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>((IHttpResponseFeature)new HttpResponseFeature());

        }
        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            var bytes = new byte[1];
            var requestFeatures = new FeatureCollection();
            var messageRequestFeature = new HttpRequestFeature();
            messageRequestFeature.Path = "/weatherforecast";
            messageRequestFeature.Body = new MemoryStream(bytes);
            messageRequestFeature.Method = "GET";
            messageRequestFeature.Headers = new HeaderDictionary();
            var responseStream = new MemoryStream();
            var responseBody = new StreamResponseBodyFeature(responseStream);
            var httpResponseFeature = new HttpResponseFeature { Body = responseStream };

            requestFeatures.Set<IHttpResponseBodyFeature>(responseBody);
            requestFeatures.Set<IHttpRequestFeature>(messageRequestFeature);
            requestFeatures.Set<IHttpResponseFeature>(httpResponseFeature);
            var context = application.CreateContext(requestFeatures);
            application.ProcessRequestAsync(context);

            return Task.Delay(100);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(100);
        }

        public IFeatureCollection Features { get; }
    }
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
            
            //this._hostedServices = this.Services.GetService<IEnumerable<IHostedService>>();
            var applicationBuilderFactory = build.Services.GetRequiredService<IApplicationBuilderFactory>();
            var contextFactory = build.Services.GetRequiredService<IHttpContextFactory>();
            var hostedServices = build.Services.GetRequiredService<IEnumerable<IHostedService>>();
            build.Run();

            var featureCollection = new FeatureCollection();
            featureCollection.Set<IHttpRequestFeature>((IHttpRequestFeature)new HttpRequestFeature());
            featureCollection.Set<IHttpResponseFeature>((IHttpResponseFeature)new HttpResponseFeature());
            var applicationBuilder = applicationBuilderFactory.CreateBuilder(featureCollection);

            //Startup.ConfigureStatic(applicationBuilder);
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
            // - UseKestrel creates a KestrelServer:IServer
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
                services.AddSingleton<IServer, MsgNThenServer>();
                //services.AddHostedService<MsgNThenService>();
            });
        }
    }
}
