using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MsgNThen.Redis.Converters
{
    static class StaticRedisSerializer
    {
        public static RedisValue RedisSerialize<T>(T val)
        {
            return JsonConvert.SerializeObject(val);
        }

        public static T RedisDeserialize<T>(RedisValue json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
