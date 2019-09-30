using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace MsgNThen.Redis.Converters
{
    static class DateConverters
    {
        public static string ExpiryToTimeString(TimeSpan expiry)
        {
            return DateTime.UtcNow.Add(expiry).ToString("s");
        }
        public static string ToRedisValue(this DateTime time)
        {
            return time.ToString("s");
        }

        public static DateTime FromRedisStringAsDateTime(RedisValue val)
        {
            return DateTime.ParseExact(val.ToString(), "s", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
