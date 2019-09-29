using System;
using System.Collections.Generic;
using MsgNThen.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Rabbit
{
    public class TaskFoo : ITaskFoo
    {
        private readonly IConnection _connection;

        public TaskFoo(IConnection connection)
        {
            _connection = connection;
        }

        public void PublishSingle(SimpleMessage message, SimpleMessage andThen)
        {
            var groupId = Guid.NewGuid();
            var ch = _connection.CreateModel();

            message.Properties.Headers["MessageGroupId"] = groupId;
            ch.BasicPublish(message.Exchange, message.RoutingKey, true, message.Properties, message.Body);
            ch.Close(Constants.ReplySuccess, "Closing the channel");
        }

        public void PublishBatch(IEnumerable<SimpleMessage> messages, SimpleMessage andThen, int mode)
        {
            var groupId = Guid.NewGuid();
            var ch = _connection.CreateModel();
            var batch = ch.CreateBasicPublishBatch();

            int messageSentCounter = 0;
            foreach (var message in messages)
            {
                message.Properties.Headers["MessageGroupId"] = groupId;
                batch.Add(message.Exchange, message.RoutingKey, true, message.Properties, message.Body);
                messageSentCounter++;
            }
            //record event in redis
            //mode 1: client sends andThen after every message so a separate handler can check the message count
            //mode 2: server sends andThen at end to separate handler that polls redis 
            //note: polling is required anyway if you want a timeout on the message group
            //mode 2 can be more efficient if there is a large number of messages
            //mode 1 means the clients can be unaware of redis

            batch.Publish();
            andThen.Properties.Headers["MessagesSent"] = messageSentCounter;
            //mark set as sent in redis and attach message to send when complete
            if (mode == 1)
            {
                //client will send andThen message after it handles each message.
                //redis.CompleteBatch(groupId, andThen)
                ch.BasicPublish(andThen.Exchange, andThen.RoutingKey, true, andThen.Properties, andThen.Body);
            }
            else if (mode == 2)
            {
                //send the andThen message to the service that will now poll redis
                ch.BasicPublish(andThen.Exchange, andThen.RoutingKey, true, andThen.Properties, andThen.Body);
            }

            ch.Close(Constants.ReplySuccess, "Closing the channel");
            
        }
    }
}