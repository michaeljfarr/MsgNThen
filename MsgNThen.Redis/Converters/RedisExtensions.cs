using System.Text;
using StackExchange.Redis;

namespace MsgNThen.Redis.Converters
{
    static class RedisExtensions
    {
        public static string ReadAsString(RedisValue value)
        {
            var boxedValue = value.Box();
            if (boxedValue != null)
            {
                if (boxedValue is byte[] bytes)
                    return Encoding.UTF8.GetString(bytes);
                else
                {
                    return boxedValue.ToString();
                }
            }
            return null;
        }
    }
}
