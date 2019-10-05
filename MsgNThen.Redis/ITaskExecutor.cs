using System;
using System.Threading.Tasks;

namespace MsgNThen.Redis
{
    public interface ITaskExecutor
    {
        TimeSpan MaxExecutionTime { get; }
        Task ExecuteAsync(PipeInfo pipeInfo, RedisPipeValue value);
        void Execute(PipeInfo pipeInfo, RedisPipeValue value);
    }
}