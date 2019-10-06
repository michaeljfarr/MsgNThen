using System;
using System.Threading;

namespace MsgNThen.Redis
{
    /// <summary>
    /// The Redis TaskReader reads tasks from IRedisTaskFunnel.ListenForPipeEvents which writes events (PipeInfo) to a
    /// BlockingCollection<PipeInfo>.  The TaskReader then finds the taskExecutor via the pipeInfo.ParentPipeName and invokes
    /// Execute on it (a synchronous method)
    /// </summary>
    public interface ITaskReader
    {
        void Start(TimeSpan lockExpiry, CancellationToken cancellationToken);
    }
}