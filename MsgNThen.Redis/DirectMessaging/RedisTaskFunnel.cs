using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MsgNThen.Redis.Abstractions;
using MsgNThen.Redis.Converters;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace MsgNThen.Redis.DirectMessaging
{
    class RedisTaskFunnel : IRedisTaskFunnel
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly ILogger<RedisTaskFunnel> _logger;

        public RedisTaskFunnel(ConnectionMultiplexer redis, ILogger<RedisTaskFunnel> logger)
        {
            _redis = redis;
            _logger = logger;
        }
        private static string CreateParentChildSetPath(string parentPipeName)
        {
            if (parentPipeName?.Contains(RedisTaskMultiplexorConstants.PathSeparator) == true)
            {
                throw new ApplicationException($"Pipenames must not contain '{RedisTaskMultiplexorConstants.PathSeparator}' but name was '{parentPipeName}'");
            }
            var childPipePath = $"{RedisTaskMultiplexorConstants.RedisTaskMultiplexorInfoPrefix}{parentPipeName}";
            return childPipePath;
        }

        public IReadOnlyList<string> GetChildPipeNames(string parentPipeName)
        {
            var db = _redis.GetDatabase();
            var parentInfoPath = CreateParentChildSetPath(parentPipeName);
            var entries = db.HashGetAll(parentInfoPath);
            if (entries == null)
            {
                return new string[0];
            }

            return entries.Select(a => RedisExtensions.ReadAsString(a.Name)).ToList();
        }


        public (bool sent, bool clients) TrySendMessage(string parentPipeName, string childPipeName, object body, int maxListLength = int.MaxValue, TimeSpan? expiry = null)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            //will throw ArgumentException is body is not a supported type
            var redisValue = RedisValue.Unbox(body);

            var db = _redis.GetDatabase();
            var parentInfoPath = CreateParentChildSetPath(parentPipeName);
            var childPipePath = PipeInfo.Create(parentPipeName, childPipeName);
            var trans = db.CreateTransaction();
            {
                if (maxListLength < int.MaxValue)
                {
                    trans.AddCondition(Condition.ListLengthLessThan(childPipePath.PipePath, maxListLength));
                }

                //ensure the name of the new pipe exists for the pipe monitor (before checking list length)
                db.SetAdd(RedisTaskMultiplexorConstants.PipeNameSetKey, parentPipeName);

                //add the child to the parents hash set (and update the expiry time on it)
                db.HashSet(parentInfoPath, childPipeName, DateConverters.ExpiryToTimeString(expiry ?? TimeSpan.FromDays(7)));

                //add the message to the left of the list, and is later popped from the right.
                db.ListLeftPush(childPipePath.PipePath, redisValue);
            }
            var executed = trans.Execute();
            if (!executed)
            {
                return (false, false);
            }

            var sub = _redis.GetSubscriber();
            var listeners = sub.Publish(childPipePath.BroadcastPath, $"{{\"type\":\"new\",\"parent\":\"{parentPipeName}\",\"child\":\"{childPipeName}\"}}");
            return (true, listeners > 0);
        }

        public bool LockExtend(RedisPipeValue value, TimeSpan lockExpiry)
        {
            var db = _redis.GetDatabase();
            return db.LockExtend(value.PipeInfo.LockPath, value.LockValue, lockExpiry);
        }

        public void LockReleaseBatch(RedisPipeBatch batch)
        {
            var db = _redis.GetDatabase();
            db.KeyDelete(batch.LockValue);
        }

        public bool LockRelease(RedisPipeValue value, bool success)
        {
            var db = _redis.GetDatabase();
            if (value.Peaked)
            {
                if (!success)
                {
                    //this is a Nack
                    db.ListLeftPush(value.PipeInfo.PipePath, value.RedisValue);
                }
            }

            return true;
        }

        static bool ArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length == a2.Length)
            {
                for (int i = 0; i < a1.Length; i++)
                {
                    if (a1[i] != a2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public void ListenForPipeEvents(/*IEnumerable<string> parentPipeNames,*/
            BlockingCollection<PipeInfo> pipeInfos)
        {
            var sub = _redis.GetSubscriber();
            //foreach (var parentPipeName in parentPipeNames){}
            sub.Subscribe(RedisTaskMultiplexorConstants.RedisTaskMultiplexorBroadcastPrefix, (channel, value) =>
            {
                var eventObject = JObject.Parse(value);
                //$"{{\"type\":\"new\",\"parent\":\"{parentPipeName}\",\"child\":\"{childPipeName}\"}}");
                var parent = eventObject["parent"].ToString();
                var child = eventObject["child"].ToString();
                var eventPipeInfo = PipeInfo.Create(parent, child);
                pipeInfos.Add(eventPipeInfo);
            });
        }

        public long GetListLength(PipeInfo pipeInfo)
        {
            var db = _redis.GetDatabase();
            var length = db.ListLength(pipeInfo.PipePath);
            return length;
        }

        public RedisPipeValue TryReadMessage(bool peak, PipeInfo pipeInfo, TimeSpan lockExpiry)
        {
            var batch = TryReadMessageBatch(peak, pipeInfo, lockExpiry, 1);
            return new RedisPipeValue(pipeInfo, batch.RedisValues.FirstOrDefault(), batch.LockValue, batch.Peaked);
        }

        public RedisPipeBatch TryReadMessageBatch(bool peak, PipeInfo pipeInfo, TimeSpan lockExpiry, int batchSize)
        {
            var db = _redis.GetDatabase();

            if (peak)
            {
                var batchAddress = $"{Environment.MachineName}:{Guid.NewGuid()}";
                
                //add the batch expiry to the hash set (and update the expiry time on it)
                db.HashSet(RedisTaskMultiplexorConstants.BatchesSetKey, batchAddress, DateConverters.ExpiryToTimeString(lockExpiry));

                var message = db.ListRightPopLeftPush(pipeInfo.PipePath, batchAddress);
                if (!message.HasValue)
                {
                    batchAddress = null;
                    return new RedisPipeBatch(pipeInfo, new RedisValue[0], null, true);
                }

                var messages = new List<RedisValue>() {message};
                for (int i = 1; i < batchSize; i++)
                {
                    message = db.ListRightPopLeftPush(pipeInfo.PipePath, batchAddress);
                    if (!message.HasValue)
                    {
                        break;
                    }

                    messages.Add(message);
                }

                return new RedisPipeBatch(pipeInfo, messages, batchAddress, true);
            }
            else
            {
                var message = db.ListRightPop(pipeInfo.PipePath);
                if (!message.HasValue)
                {
                    return new RedisPipeBatch(pipeInfo, new RedisValue[0], null, true);
                }
                var messages = new List<RedisValue>() { message };
                for (int i = 1; i < batchSize; i++)
                {
                    message = db.ListRightPop(pipeInfo.PipePath);
                    if (!message.HasValue)
                    {
                        break;
                    }

                    messages.Add(message);
                }
                return new RedisPipeBatch(pipeInfo, messages, null, false);
            }
        }
    }
}