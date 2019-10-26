using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;
using Xunit;

namespace MsgNThen.Adapter.Tests
{
    public class MsgnThenInitiator : IMessageHandler
    {
        private readonly ILogger<MsgnThenInitiator> _logger;
        private readonly RequestDelegate _requestDelegate;

        public MsgnThenInitiator(ILogger<MsgnThenInitiator> logger, RequestDelegate requestDelegate)
        {
            _logger = logger;
            _requestDelegate = requestDelegate;
        }

        public async Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            try
            {
                var msgnThenContext = new MsgNThenContext();
                foreach (var property in message.Properties.Headers)
                {
                    msgnThenContext.Request.Headers[property.Key] = property.Value?.ToString();
                }
                msgnThenContext.Request.Headers["MessageId"] = message.Properties.MessageId;
                msgnThenContext.Request.Headers["AppId"] = message.Properties.AppId;
                msgnThenContext.Request.Headers["ReplyTo"] = message.Properties.ReplyTo;
                msgnThenContext.Request.Headers["CorrelationId"] = message.Properties.CorrelationId;
                //note: https://www.rabbitmq.com/validated-user-id.html
                msgnThenContext.Request.Headers["UserId"] = message.Properties.UserId;
                //.Headers["MessageId"] = message.Properties.ContentEncoding
                var ms = new MemoryStream(message.Body);
                msgnThenContext.Request.ContentLength = message.Body.LongLength;
                msgnThenContext.Request.Body = ms;
                msgnThenContext.Request.ContentType = message.Properties.ContentType;
                await _requestDelegate(msgnThenContext);
                return AckResult.Ack;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleMessageTask Failed");
                return AckResult.NackQuit;
            }
        }
    }
    public class MsgnThenBuilderTests
    {

        [Fact]
        public void TestAppBuilder()
        {
            var serviceProvider = CreateServiceProvider();
            var messageGroupHandler = serviceProvider.GetRequiredService<IMessageGroupHandler>();
            var logger = serviceProvider.GetRequiredService<ILogger<MsgnThenInitiator>>();

            //this copy/paste implementation was for my own understanding about how much of a system that is fundamentally designed for HTTP
            //could be used with RabbitMQ bindings.  If this http middleware actually fits the requirement, we could either use HostingApplication or IHttpContextFactory.
            var msgnThenBuilder = new PipelineBuilder(serviceProvider);
            msgnThenBuilder.Use(async (context, next) =>
            {
                var messageIdString = context.Request.Headers["MessageId"];
                var groupIdString = context.Request.Headers["GroupId"];
                if (!Guid.TryParse(messageIdString, out var messageId) ||
                    !Guid.TryParse(groupIdString, out var groupId))
                {
                    logger.LogError($"Invalid MessageId({messageIdString}) or GroupId({groupIdString})");
                    return;
                }
                // Do work that doesn't write to the Response.
                if (next!=null)
                {
                    await next.Invoke();
                }
                // Do logging or other work that doesn't write to the Response.
                messageGroupHandler.MessageHandled(groupId, messageId);
            });
            var testDelegate = msgnThenBuilder.Build();
            var initator = new MsgnThenInitiator(logger, testDelegate);
            var msgnThenContext = new MsgNThenContext();
            testDelegate.Invoke(msgnThenContext);
        }

        public static ServiceProvider CreateServiceProvider()
        {
            var collection = BasicIntegrationTests.CreateServiceCollection();
            collection.AddSingleton<IMessageHandler, MsgnThenInitiator>();
            
            return collection.BuildServiceProvider();
        }
    }
}