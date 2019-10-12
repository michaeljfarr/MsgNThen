using System.Collections.Generic;
using StackExchange.Redis;

namespace MsgNThen.Redis.Abstractions
{
    public class RedisPipeBatch
    {
        public RedisPipeBatch(PipeInfo pipeInfo, IReadOnlyList<RedisValue> redisValues, string holdingList, bool peaked)
        {
            PipeInfo = pipeInfo;
            RedisValues = redisValues;
            HoldingList = holdingList;
            Peaked = peaked;
        }

        public PipeInfo PipeInfo { get; }
        public IReadOnlyList<RedisValue> RedisValues { get; }
        public IEnumerable<RedisPipeValue> RedisPipeValues
        {
            get
            {
                foreach (var redisValue in RedisValues)
                {
                    yield return GetAsRedisPipeValue(redisValue);
                }
            }
        }
        public string HoldingList { get; private set; }
        public bool Peaked { get; }

        private RedisPipeValue GetAsRedisPipeValue(RedisValue redisValue)
        {
            return new RedisPipeValue(PipeInfo, redisValue, HoldingList, Peaked);
        }
    }
}