using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MsgNThen.Redis
{
    public class RedisMonitor
    {
        private readonly ILogger<RedisMonitor> _logger;

        public RedisMonitor(ConnectionMultiplexer redis, ILogger<RedisMonitor> logger)
        {
            _logger = logger;
            redis.ConfigurationChanged += _redis_ConfigurationChanged;
            redis.ConnectionFailed += _redis_ConnectionFailed;
            redis.ConnectionRestored += _redis_ConnectionRestored;
            redis.ErrorMessage += _redis_ErrorMessage;
            redis.InternalError += _redis_InternalError;
            redis.HashSlotMoved += _redis_HashSlotMoved;
        }

        private void _redis_HashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            _logger.LogWarning($"Redis Error: {e?.HashSlot}, {e?.NewEndPoint}, {e?.OldEndPoint}");
        }

        private void _redis_InternalError(object sender, InternalErrorEventArgs e)
        {
            _logger.LogWarning(e?.Exception, $"Redis Error: {e?.EndPoint}, {e?.ConnectionType}, {e?.Origin}");
        }

        private void _redis_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            _logger.LogWarning($"Redis Error: {e?.EndPoint}, {e?.Message}");
        }

        private void _redis_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogWarning(e?.Exception, $"Redis Restored: {e?.FailureType}, {e?.ConnectionType}, {e?.EndPoint}");
        }

        private void _redis_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogWarning(e?.Exception, $"Redis Fail: {e?.FailureType}, {e?.ConnectionType}, {e?.EndPoint}");
        }

        private void _redis_ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            _logger.LogWarning($"Redis Config: {e?.EndPoint}");
        }
    }
}