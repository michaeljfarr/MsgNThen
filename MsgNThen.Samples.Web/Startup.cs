using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Adapter;
using MsgNThen.Rabbit;
using MsgNThen.Redis;

namespace MsgNThen.Samples.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddControllers();

            //either rabbit, sqs or the Redis queue implementation is required
            services.ConfigureRabbit(Configuration);
            services.AddMsgnThenRabbit();

            //RedisTask reader will be an alternative to rabbit, but currently its TaskReader doesn't yet call IMessageHandler.HandleMessageTask
            services.AddRedisTaskReader();

            //Redis is required if MsgNThen message dependencies are required (send or receive) or for its TaskReader, otherwise it can be left out
            services.ConfigureRedis(Configuration);
            services.AddRedisFactory();
            services.AddRedisNThenEventHandler();

            //redis monitor is not required at all, and really should not be included in production code.
            services.AddRedisMonitor();

            //we haven't yet built the routing logic to deliver messages to different locations.
            //although it would be trivial to map the outgoing messages via Rabbit.
            services.AddDummyAndThenMessageDeliverer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            ConfigureStatic(app);
        }
        public static void ConfigureStatic(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
