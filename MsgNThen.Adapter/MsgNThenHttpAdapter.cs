﻿using System;
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
                //we may revert to the other approach.

                //Typical webserver is a lot more complicated because it needs to handle the http protocol.HttpProtocol.ProcessRequests
                //and can handle multiple requests via the same connection.  MsgNThen is entirely reliant on RabbitMQ client to handle all
                //such issues.
                //The Protocol can't directly access the HttpContext itself (which is really strange at first glance). Instead it implements
                //"IFeature"s that the HttpContext uses to implement its properties.

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

                //todo: implement result forwarding (using something like IAndThenMessageDeliverer)
                //use Redis and S3 as result stores so that the final andThen can use the data collected from
                //those requests.

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
            //todo: make string constants (after work on generic listener and URI delivery method)
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
            //todo: add listener code, preferably using generic Uri/endpoint system.
            //todo: make SimpleMessage more generic, use Uris to:
            // + target SQS, RabbitMQ, Redis, Http endpoints
            // + will use dictionary to make scheme handlers
        }

        public void StopListening()
        {
            //todo: stop listener here.
        }
    }
}