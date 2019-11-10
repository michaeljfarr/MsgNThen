using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MsgNThen.Interfaces;
using MsgNThen.Redis.Abstractions;

namespace MsgNThen.Redis.DirectMessaging
{
    class RedisDeliveryScheme : IUriDeliveryScheme
    {
        private readonly IRedisTaskFunnel _redisTaskFunnel;

        public RedisDeliveryScheme(IRedisTaskFunnel redisTaskFunnel)
        {
            _redisTaskFunnel = redisTaskFunnel;
        }

        public string Scheme => "redis";
        public Task Deliver(Uri destination, MsgNThenMessage message)
        {
            // (bool sent, bool clients) TrySendMessage(string parentPipeName, string childPipeName, object body,
            //    int maxListLength = int.MaxValue, TimeSpan ? expiry = null);
            //_redisTaskFunnel.TrySendMessage()

            //redis://<ignored>/<parent>/<child>
            if (destination.Segments.Length != 3)
            {
                throw new Exception($"redis uri didn't conform to the correct structure {destination}.");
            }
            var parent = destination.Segments[1].TrimEnd('/');
            var child = destination.Segments[2];

            var body = ((MemoryStream)message.Body).ToArray();
            _redisTaskFunnel.TrySendMessage(parent, child, body);
            return Task.CompletedTask;

        }

        private static void DoAssignment(string message, Action<string> basicProperties)
        {
            if (message != null)
            {
                basicProperties(message);
            }
        }
    }
}