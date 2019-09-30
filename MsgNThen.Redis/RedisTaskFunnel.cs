using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MsgNThen.Redis.Converters;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace MsgNThen.Redis
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

                //add the message to the list
                db.ListRightPush(childPipePath.PipePath, redisValue);
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
        public bool LockRelease(RedisPipeValue value, bool success)
        {
            var lockRetained = LockExtend(value, TimeSpan.FromMinutes(30));
            if (!lockRetained)
            {
                throw new ApplicationException($"Could not retain lock for {value.PipeInfo.LockPath} whilst trying to release.");
            }
            //since we have the lock, we should safely be able to pop the item from the queue.
            var db = _redis.GetDatabase();
            var popped = false;
            
            if (success && value.Peaked)
            {
                var poppedValue = db.ListLeftPop(value.PipeInfo.PipePath);
                popped = poppedValue.HasValue;
                if (popped)
                {
                    var same = poppedValue.CompareTo(value.RedisValue) == 0;
                    if (!same)
                    {
                        //dont put the message back on the end of the queue
                        //we don't have a way to detect permanently failing message
                        //db.ListRightPush(value.PipeInfo.PipePath, poppedValue);
                        _logger.LogError($"Queue {value.PipeInfo.PipePath} was different when releasing the lock, we did not put it back!");
                    }
                }
            }
            var unlocked = db.LockRelease(value.PipeInfo.LockPath, value.LockValue);
            if (success && !popped)
            {
                _logger.LogError($"Queue {value.PipeInfo.LockPath} was entirely empty when releasing the lock!");
            }

            if (!unlocked)
            {
                _logger.LogError($"Failed to release lock {value.PipeInfo.PipePath} at end of LockRelease!");
            }

            return unlocked;
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

        public void ListenForPipeEvents(BlockingCollection<PipeInfo> pipeInfos)
        {
            var sub = _redis.GetSubscriber();
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
            var db = _redis.GetDatabase();
            var lockValue = $"{Environment.MachineName}:{Guid.NewGuid()}";
            var lockInfo = db.LockTake(pipeInfo.LockPath, lockValue, lockExpiry);

            if (!lockInfo)
            {
                return null;
            }

            if (peak)
            {
                var message = db.ListGetByIndex(pipeInfo.PipePath, 0);
                if (!message.HasValue)
                {
                    db.LockRelease(pipeInfo.LockPath, lockValue);
                    lockValue = null;
                }
                return new RedisPipeValue(pipeInfo, message, lockValue, true);
            }
            else
            {
                var message = db.ListLeftPop(pipeInfo.PipePath);
                if (!message.HasValue)
                {
                    db.LockRelease(pipeInfo.LockPath, lockValue);
                    lockValue = null;
                }
                return new RedisPipeValue(pipeInfo, message, lockValue, false);
            }
        }
    }
}