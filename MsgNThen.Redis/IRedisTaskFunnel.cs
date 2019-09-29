using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MsgNThen.Redis
{
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
        void ListenForPipeEvents(BlockingCollection<PipeInfo> pipeInfos);
        long GetListLength(PipeInfo pipeInfo);
    }
}