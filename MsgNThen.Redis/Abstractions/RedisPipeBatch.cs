using System.Collections.Generic;
using StackExchange.Redis;

namespace MsgNThen.Redis.Abstractions
{
    public class RedisPipeBatch
    {
        public RedisPipeBatch(PipeInfo pipeInfo, IReadOnlyList<RedisValue> redisValues, string lockValue, bool peaked)
        {
            PipeInfo = pipeInfo;
            RedisValues = redisValues;
            LockValue = lockValue;
            Peaked = peaked;
        }

        public PipeInfo PipeInfo { get; }
        public IReadOnlyList<RedisValue> RedisValues { get; }
        public string LockValue { get; private set; }
        public bool Peaked { get; }
    }
}