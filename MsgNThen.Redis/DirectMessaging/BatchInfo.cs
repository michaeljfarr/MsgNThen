using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgNThen.Redis.Converters;
using StackExchange.Redis;

namespace MsgNThen.Redis.DirectMessaging
{
    static class BatchInfo
    {
        public static RedisValue MakeBatchInfo(TimeSpan expiry, string pipePath)
        {
            return $"{DateConverters.ExpiryToTimeString(expiry)}|{pipePath}";
        }

        public static (DateTime expiry, string pipePath) ReadBatchInfo(RedisValue value)
        {
            var parts = value.ToString().Split('|');
            return (DateConverters.FromRedisStringAsDateTime(parts.FirstOrDefault()), parts.Skip(1).FirstOrDefault());
        }
    }
}
