using System;
using System.Threading.Tasks;

namespace MsgNThen.Redis.Tests
{
    class ConsoleWriterTaskExecutor : ITaskExecutor
    {
        public TimeSpan MaxExecutionTime => TimeSpan.FromMinutes(1);
        public Task ExecuteAsync(PipeInfo pipeInfo, RedisPipeValue value)
        {
            Execute(pipeInfo, value);
            return Task.CompletedTask;
        }

        public void Execute(PipeInfo pipeInfo, RedisPipeValue value)
        {
            var valueAsString = value.ValueString;
            System.Diagnostics.Debug.WriteLine($"{pipeInfo.ParentPipeName}/{pipeInfo.ChildPipeName} : {valueAsString}");
        }
    }
}