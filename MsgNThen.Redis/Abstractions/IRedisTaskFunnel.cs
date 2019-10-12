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
    /// New Peak Mechanism
    ///  - The TryReadMessage 'peak' moves the message from the primary queue onto a batch specific queue, which is destroyed if the batch succeeds, or is pushed
    ///      back on to the origin queue if not.  Each batch is hash value that stores the location and a timestamp which indicates the resubmission time.
    ///    Readers polls the this set of batch queues to resubmit a batch.
    ///    Because the list is destroyed as a batch (although individuals can be resent), it might be difficult to manage a consistent level of task parallelism.
    ///    Currently throughput is about 1000/sec, which might be just enough for what we are trying to achieve.  Hopefully we can improve that further though.
    /// 
    ///  Early Peak Mechanism
    ///  - The TryReadMessage 'peak' will leave the message inside the queue and acquire a lock to restrict other clients trying to read at the same time.
    ///    - It provides a very easy recovery if the client crashes, because the lock will just expire
    ///    - The lock is not used if peek is false, and so it will got a lot faster.
    ///
    /// See this implementation for a simpler version on how this should work: https://gist.github.com/tenowg/c5de38cb1027ab875e56
    /// </remarks>
    public interface IRedisTaskFunnel
    {
        /// <summary>
        /// Send a message via queue parentPipeName which will be processed asynchronously into pipes that will be processed sequentially.
        /// </summary>
        (bool sent, bool clients) TrySendMessage(string parentPipeName, string childPipeName, object body,
            int maxListLength = int.MaxValue, TimeSpan? expiry = null);

        bool RetainHoldingList(RedisPipeBatch value, TimeSpan lockExpiry);
        bool RetainHoldingList(RedisPipeValue value, TimeSpan lockExpiry);
        void AfterExecuteBatch(RedisPipeBatch batch);
        void AfterExecute(RedisPipeValue value, bool success);

        // <summary>
        // Calls TryReadMessageBatch with a batch size of 1.
        // </summary>
        //RedisPipeValue TryReadMessage(bool peak, PipeInfo pipeInfo, TimeSpan lockExpiry);

        /// <summary>
        /// Reads up to batchSize messages from a list message with the following guarantees:
        ///  - this will be the only process to read the items.
        ///  - if peak is false, the items will be atomically moved on another list before returning.
        /// </summary>
        /// <remarks>
        ///  - The caller should call AfterExecute after each message to ensure that failed messages are resubmitted.
        ///  - The caller should also call AfterExecuteBatch after all messages to ensure that the batch is destroyed.
        ///  - If the message isn't processed in before  "lockExpiry" of time passes the caller should call RetainHoldingList.
        /// </remarks>
        RedisPipeBatch TryReadMessageBatch(bool peak, PipeInfo pipeInfo, TimeSpan lockExpiry, int batchSize);
        IReadOnlyList<string> GetChildPipeNames(string parentPipeName);
        void ListenForPipeEvents(/*IEnumerable<string> parentPipeNames,*/
            BlockingCollection<PipeInfo> pipeInfos);
        long GetListLength(PipeInfo pipeInfo);
        void DestroyChildPipe(PipeInfo pipeInfo);
    }
}