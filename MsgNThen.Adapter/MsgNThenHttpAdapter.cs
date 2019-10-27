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
        private readonly CancellationToken _cancellationToken;

        public MsgNThenHttpAdapter(ILogger logger, IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _logger = logger;
            _application = application;
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
                //todo: fix performance
                var bytes = message.Body;
                var requestFeatures = new FeatureCollection();
                var messageRequestFeature = new HttpRequestFeature
                {
                    Path = "/weatherforecast",
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
                return AckResult.Ack;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleMessageTask Failed");
                return AckResult.NackQuit;
            }
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