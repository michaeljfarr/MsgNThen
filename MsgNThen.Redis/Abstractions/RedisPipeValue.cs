using System;
using MsgNThen.Redis.Converters;
using StackExchange.Redis;

namespace MsgNThen.Redis.Abstractions
{
    public class RedisPipeValue
    {
        private readonly RedisValue _redisValue;
        private readonly Lazy<object> _convertedValue;

        public RedisPipeValue(PipeInfo pipeInfo, RedisValue redisValue, string batchHoldingList, bool peaked)
        {
            _redisValue = redisValue;
            _convertedValue = new Lazy<object>(() =>
            {
                var value = _redisValue.Box();
                var objectValue = value;
                return objectValue;
            });
            PipeInfo = pipeInfo;
            BatchHoldingList = batchHoldingList;
            Peaked = peaked;
        }
        internal RedisValue RedisValue => _redisValue;
        public string ValueString => RedisExtensions.ReadAsString(_redisValue);
        public object Value => _convertedValue.Value;
        public PipeInfo PipeInfo { get; }
        public string BatchHoldingList { get; private set; }
        public bool Peaked { get; }
    }
}