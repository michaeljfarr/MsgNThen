using System;
using StackExchange.Redis;

namespace MsgNThen.Redis
{
    public class RedisPipeValue
    {
        private readonly RedisValue _redisValue;
        private readonly Lazy<object> _convertedValue;

        public RedisPipeValue(PipeInfo pipeInfo, RedisValue redisValue, string lockValue, bool peaked)
        {
            _redisValue = redisValue;
            _convertedValue = new Lazy<object>(() =>
            {
                var value = _redisValue.Box();
                var objectValue = value;
                return objectValue;
            });
            PipeInfo = pipeInfo;
            LockValue = lockValue;
            Peaked = peaked;
        }
        internal RedisValue RedisValue => _redisValue;
        public string ValueString => RedisExtensions.ReadAsString(_redisValue);
        public object Value => _convertedValue.Value;
        public PipeInfo PipeInfo { get; }
        public string LockValue { get; private set; }
        public bool Peaked { get; }
    }
}