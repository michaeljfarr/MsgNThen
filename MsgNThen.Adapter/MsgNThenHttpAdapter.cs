using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MsgNThen.Interfaces;

namespace MsgNThen.Adapter
{
    public class MsgNThenHttpAdapter<TContext> : IMsgNThenHttpAdapter
    {
        private readonly ILogger _logger;
        private readonly IHttpApplication<TContext> _application;
        private readonly IMessageGroupHandler _messageGroupHandler;
        private readonly CancellationToken _cancellationToken;

        public MsgNThenHttpAdapter(ILogger logger, CancellationToken cancellationToken,
            IHttpApplication<TContext> application, IMessageGroupHandler messageGroupHandler)
        {
            _logger = logger;
            _application = application;
            _messageGroupHandler = messageGroupHandler;
            _cancellationToken = cancellationToken;
        }

        public async Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return AckResult.NackRequeue;
            }
            try
            {
                var bytes = message.Body;

                //note: This may not be the correct way to implement the FeatureCollection.  In most of the available samples, 
                //features are static and the HttpContext is customized so it can be manipulated directly within this method.
                //For the time being, this approach is very easy and provides better decoupling.  When things get more complicated
                //we may rever to the other approach.
                var requestFeatures = new FeatureCollection();
                var messageRequestFeature = new HttpRequestFeature
                {
                    Path = $"/{message.RoutingKey}",
                    Body = new MemoryStream(bytes),
                    Method = "GET",
                    Headers = new HeaderDictionary(),
                };
                if (message.Properties.Headers != null)
                {
                    foreach (var property in message.Properties.Headers)
                    {
                        messageRequestFeature.Headers[property.Key] = property.Value?.ToString();
                    }
                }

                var groupInfo = GetMessageGroupInfo(message);


                messageRequestFeature.Headers["MessageId"] = message.Properties.MessageId;
                messageRequestFeature.Headers["AppId"] = message.Properties.AppId;
                messageRequestFeature.Headers["ReplyTo"] = message.Properties.ReplyTo;
                messageRequestFeature.Headers["CorrelationId"] = message.Properties.CorrelationId;
                //note: https://www.rabbitmq.com/validated-user-id.html
                messageRequestFeature.Headers["UserId"] = message.Properties.UserId;
                messageRequestFeature.Headers.ContentLength = message.Body.LongLength;
                messageRequestFeature.Headers[HeaderNames.ContentType] = message.Properties.ContentType;

                var responseStream = new MemoryStream();
                var responseBody = new StreamResponseBodyFeature(responseStream);
                var httpResponseFeature = new HttpResponseFeature { Body = responseStream };
                requestFeatures.Set<IHttpResponseBodyFeature>(responseBody);
                requestFeatures.Set<IHttpRequestFeature>(messageRequestFeature);
                requestFeatures.Set<IHttpResponseFeature>(httpResponseFeature);
                var context = _application.CreateContext(requestFeatures);
                await _application.ProcessRequestAsync(context);

                if (groupInfo != null)
                {
                    _messageGroupHandler.MessageHandled(groupInfo.Value.messageGroupId, groupInfo.Value.messageId);
                }

                return AckResult.Ack;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleMessageTask Failed");
                return AckResult.NackQuit;
            }
        }

        private static (Guid messageGroupId, Guid messageId)? GetMessageGroupInfo(IHandledMessage message)
        {
            object groupIdObj = null;
            if ((message.Properties.Headers?.TryGetValue("MessageGroupId", out groupIdObj) ?? false) &&
                (groupIdObj is string groupIdStr) &&
                Guid.TryParse(groupIdStr, out var messageGroupId) &&
                Guid.TryParse(message.Properties.MessageId, out var messageId))
            {
                return (messageGroupId, messageId);
            }

            return null;
        }

        public void Listen()
        {
            //throw new NotImplementedException();
        }

        public void StopListening()
        {
            //throw new NotImplementedException();
        }
    }
}