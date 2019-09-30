using MsgNThen.Interfaces;
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

    public interface IMessageDeliverer
    {
        void Deliver(SimpleMessage andThen);
    }

    public interface IMessageGroupHandler
    {
        void StartMessageGroup(Guid groupId);
        void SetMessageGroupCount(Guid groupId, int messageCount, SimpleMessage andThen);
        void CompleteMessageGroupTransmission(Guid groupId);
        void MessageHandled(Guid groupId, Guid messageId);
    }
}