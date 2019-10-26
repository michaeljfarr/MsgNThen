using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MsgNThen.Samples.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            ConfigureStatic(app);
        }
        public static void ConfigureStatic(IApplicationBuilder app)
        {

            app.Use(async (context, next) =>
            {
                var messageIdString = context.Request.Headers["MessageId"];
                var groupIdString = context.Request.Headers["GroupId"];
                if (!Guid.TryParse(messageIdString, out var messageId) ||
                    !Guid.TryParse(groupIdString, out var groupId))
                {
                    //logger.LogError($"Invalid MessageId({messageIdString}) or GroupId({groupIdString})");
                    //return;
                }
                // Do work that doesn't write to the Response.
                if (next != null)
                {
                    await next.Invoke();
                }
                // Do logging or other work that doesn't write to the Response.
                //messageGroupHandler.MessageHandled(groupId, messageId);
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
