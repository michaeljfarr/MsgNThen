namespace MsgNThen.Redis
{
    static class RedisTaskMultiplexorConstants
    {
        public const string PathSeparator = "/";
        public const string RedisTaskMultiplexorLockPrefix = "RTL" + PathSeparator;
        public const string RedisTaskMultiplexorInfoPrefix = "RTC" + PathSeparator;
        public const string RedisTaskMultiplexorPipePrefix = "RTP" + PathSeparator;
        public const string RedisTaskMultiplexorBroadcastPrefix = "RTB" + PathSeparator;
        public const string PipeNameSetKey = "RTT" + PathSeparator + "pipenames";


        public const string MessageGroupHashKeyPrefix = "MGQ" + PathSeparator + "group" + PathSeparator;
        public const string MessageGroupQueueKey = "MGQ" + PathSeparator + "groupid";
        public const string MessageGroupMsgIdSetKeyPrefix = "MGQ" + PathSeparator + "msgids" + PathSeparator;


    }
}