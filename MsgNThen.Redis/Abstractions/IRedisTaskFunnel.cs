using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MsgNThen.Redis.Abstractions
{
    /// <summary>
    /// RedisTaskFunnels are a lightweight message queue system based on redis.  Messages are added to a redis "list" in a 2 level
    /// hierarchy.  Each message has a parent and child "pipe name", the redis list name is the concatenation of those names.
    /// </summary>
    /// <remarks>
    ///  - The task funnel includes tracking for the child pipes nested within a parent.
    ///  - Redis pub/sub is used to publish notifications of messaging being sent, this is a slight overhead but it polling in the client
    ///      and reduces latency.
    ///  - Redis pub/sub is currently a broadcast of _any_ queue, which may be inefficient for clients that listen to quiet queues within a db containing a noisy one.
    ///  - Messages are kept in redis until a client reads them, clients will rediscover queues and subscriptions when they restart.
    /// </remarks>
    public interface IRedisTaskFunnel
    {
        /// <summary>
        /// Send a message via queue parentPipeName which will be processed asynchronously into pipes that will be processed sequentially.
        /// </summary>
        (bool sent, bool clients) TrySendMessage(string parentPipeName, string childPipeName, object body,
            int maxListLength = int.MaxValue, TimeSpan? expiry = null);

        bool LockExtend(RedisPipeValue value, TimeSpan lockExpiry);
        bool LockRelease(RedisPipeValue value, bool success);

        /// <summary>
        /// Receive a message from a specific child pipe, ensuring that no other message is currently being processed from the same pipe.
        /// If a lock can be created for the childPipe, either pop or peak a message from the left.
        /// </summary>
        /// <remarks>
        /// The caller must correctly use LockExtend and LockRelease to ensure the lock is maintained otherwise the message will be handled
        /// by someone else.
        /// If peak is true, the message stays on the queue until the caller releases it - otherwise the message is immediately removed.
        /// Also, issues with infrastructure can lead to the lock being lost, so in general it is important to assume that messages
        /// will be processed at least once.
        /// </remarks>
        RedisPipeValue TryReadMessage(bool peak, PipeInfo pipeInfo,
            TimeSpan lockExpiry);

        IReadOnlyList<string> GetChildPipeNames(string parentPipeName);
        void ListenForPipeEvents(/*IEnumerable<string> parentPipeNames,*/
            BlockingCollection<PipeInfo> pipeInfos);
        long GetListLength(PipeInfo pipeInfo);
    }
}