using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;
using MsgNThen.Redis.Converters;
using StackExchange.Redis;

namespace MsgNThen.Redis.NThen
{
    class MessageGroupEventHandler : IMessageGroupHandler
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IAndThenMessageDeliverer _andThenMessageDeliverer;
        private readonly ILogger<MessageGroupEventHandler> _logger;

        public MessageGroupEventHandler(ConnectionMultiplexer redis, IAndThenMessageDeliverer andThenMessageDeliverer, ILogger<MessageGroupEventHandler> logger)
        {
            _redis = redis;
            _andThenMessageDeliverer = andThenMessageDeliverer;
            _logger = logger;
        }



        private class MessageGroupExpiry
        {
            public Guid G { get; set; }
            public DateTime T { get; set; }
        }

        public void StartMessageGroup(Guid groupId)
        {
            var db = _redis.GetDatabase();
            db.ListRightPush(RedisTaskMultiplexorConstants.MessageGroupQueueKey, groupId.ToByteArray());
            var messageGroupHashKey = RedisTaskMultiplexorConstants.MessageGroupHashKeyPrefix + groupId;

            db.HashSet(messageGroupHashKey, new HashEntry[]{ new HashEntry("Created", DateTime.UtcNow.ToRedisValue())});
        }

        public void SetMessageGroupCount(Guid groupId, int messageCount, SimpleMessage andThen)
        {
            var db = _redis.GetDatabase();
            db.ListRightPush(RedisTaskMultiplexorConstants.MessageGroupQueueKey, groupId.ToByteArray());
            var messageGroupHashKey = RedisTaskMultiplexorConstants.MessageGroupHashKeyPrefix + groupId;
            db.HashSet(messageGroupHashKey, new HashEntry[]
            {
                new HashEntry("MsgCount", messageCount),
                new HashEntry("Handled", 0),
                new HashEntry("Prepared", DateTime.UtcNow.ToRedisValue()),
                new HashEntry("AndThen", StaticRedisSerializer.RedisSerialize(andThen)),
            });
        }

        public void CompleteMessageGroupTransmission(Guid groupId)
        {
            var db = _redis.GetDatabase();
            db.ListRightPush(RedisTaskMultiplexorConstants.MessageGroupQueueKey, groupId.ToByteArray());
            var messageGroupHashKey = RedisTaskMultiplexorConstants.MessageGroupHashKeyPrefix + groupId;
            db.HashSet(messageGroupHashKey, "Transmitted", DateTime.UtcNow.ToRedisValue());
        }

        public void MessagesPrepared(Guid groupId, IEnumerable<Guid> messages)
        {
            var db = _redis.GetDatabase();
            var msgIdsKey = RedisTaskMultiplexorConstants.MessageGroupMsgIdSetKeyPrefix + groupId;
            db.SetAdd(msgIdsKey, messages.Select(a => (RedisValue)a.ToByteArray()).ToArray());

        }

        public void MessageHandled(Guid groupId, Guid messageId)
        {
            var db = _redis.GetDatabase();
            var messageGroupHashKey = RedisTaskMultiplexorConstants.MessageGroupHashKeyPrefix + groupId;
            var msgIdsKey = RedisTaskMultiplexorConstants.MessageGroupMsgIdSetKeyPrefix + groupId;
            
            //using the count alone is insufficient if a message handled in an at-most-once configuration
            db.HashIncrement(messageGroupHashKey, "Handled");
            //so we need a set, although this significantly increases memory requirements and bandwidth to redis
            db.SetRemove(msgIdsKey, messageId.ToByteArray());

            if (db.SetLength(msgIdsKey) == 0)
            {
                LastMessageHandled(groupId);
            }
        }

        //todo: add poll MessageGroupQueue
        // if completed, delete straight away
        // if last message handled, deliver and then delete
        // if no message deleted or handled, delete after 1 hour

        private void LastMessageHandled(Guid groupId)
        {
            var db = _redis.GetDatabase();
            var messageGroupHashKey = RedisTaskMultiplexorConstants.MessageGroupHashKeyPrefix + groupId;
            var completed = db.HashGet(messageGroupHashKey, "Completed");
            if (completed.HasValue)
            {
                return;
            }
            var andThen = db.HashGet(messageGroupHashKey, "AndThen");
            var andThenObj = StaticRedisSerializer.RedisDeserialize<SimpleMessage>(andThen);
            _andThenMessageDeliverer.Deliver(andThenObj);
            db.HashSet(messageGroupHashKey, "Completed", DateTime.UtcNow.ToRedisValue());
        }
    }
}
