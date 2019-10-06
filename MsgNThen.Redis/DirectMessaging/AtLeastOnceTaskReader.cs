using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MsgNThen.Redis.Abstractions;
using MsgNThen.Redis.Converters;

namespace MsgNThen.Redis.DirectMessaging
{
    class AtLeastOnceTaskReader : ITaskReader
    {
        private readonly ILogger<AtLeastOnceTaskReader> _logger;
        private readonly IRedisTaskFunnel _taskFunnel;
        private readonly Dictionary<string, ITaskExecutor> _taskExecutors;
        private readonly int _maxThreads;
        private BlockingCollection<PipeInfo> _pipeInfos = null;

        /// <summary>
        /// Sets up the executors by connecting a subscriber for each message queue.  The redis implementation
        /// will analyze the existing 'pipes' and begin processing any existing messages.
        /// </summary>
        public AtLeastOnceTaskReader(ILogger<AtLeastOnceTaskReader> logger, IRedisTaskFunnel taskFunnel, Dictionary<string, ITaskExecutor> taskExecutors, int maxThreads)
        {
            if (taskExecutors == null || taskExecutors.Count <= 0)
            {
                throw new ArgumentException($"{nameof(taskExecutors)} must contain values");
            }

            _logger = logger;
            _taskFunnel = taskFunnel;
            _taskExecutors = taskExecutors;
            _maxThreads = maxThreads;
        }

        public void Start(TimeSpan lockExpiry, CancellationToken cancellationToken)
        {
            if (_pipeInfos != null)
            {
                throw new ApplicationException("Reader instance already started");
            }
            _pipeInfos = new BlockingCollection<PipeInfo>();
            _taskFunnel.ListenForPipeEvents(/*_taskExecutors.Keys, */_pipeInfos);
            ExecuteExisting(lockExpiry);
            Parallel.ForEach(_pipeInfos.GetConsumingPartitioner(cancellationToken), new ParallelOptions() { MaxDegreeOfParallelism = _maxThreads, CancellationToken = cancellationToken }, pipeInfo =>
              {
                  var taskExecutor = _taskExecutors[pipeInfo.ParentPipeName];
                  var message = _taskFunnel.TryReadMessage(true, pipeInfo, lockExpiry);
                  taskExecutor.Execute(pipeInfo, message);
                  _taskFunnel.LockRelease(message, true);
              });
        }

        private void ExecuteExisting(TimeSpan lockExpiry)
        {
            var sourcePipes = GetSourcePipes();
            var tasks = new List<Task>();
            foreach (var sourcePipe in sourcePipes)
            {
                foreach (var sourcePipeChild in sourcePipe.Value)
                {
                    var pipeInfo = PipeInfo.Create(sourcePipe.Key, sourcePipeChild);
                    //capture the number of reads write now to ignore:
                    // - new events that arrive
                    // - events that fail and are re-added.
                    var maxReads = _taskFunnel.GetListLength(pipeInfo);
                    var batchSize = 1;
                    var messageBatch = _taskFunnel.TryReadMessageBatch(true, pipeInfo, lockExpiry, batchSize);

                    //this is still an early implementation.  The objectives are:
                    //  - speed (obviously)
                    //  - consistent number of active tasks.
                    //  - batches released as soon as the last one completes

                    while (messageBatch?.LockValue != null)
                    {
                        var taskExecutor = _taskExecutors[sourcePipe.Key];
                        foreach (var message in messageBatch.RedisValues)
                        {
                            var redisMessage = new RedisPipeValue(messageBatch.PipeInfo, message, messageBatch.LockValue, messageBatch.Peaked);
                            try
                            {
                                taskExecutor.Execute(pipeInfo, redisMessage);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e,
                                    $"Error handling from {pipeInfo.ParentPipeName}/{pipeInfo.ChildPipeName}");
                                _taskFunnel.LockRelease(redisMessage, false);
                            }
                        }

                        _taskFunnel.LockReleaseBatch(messageBatch);
                        messageBatch = _taskFunnel.TryReadMessageBatch(true, pipeInfo, lockExpiry, batchSize);

                    }
                }
            }
        }

        private Dictionary<string, IReadOnlyList<string>> GetSourcePipes()
        {
            var sourcePipes = new Dictionary<string, IReadOnlyList<string>>();
            foreach (var taskExecutor in _taskExecutors)
            {
                var childPipeNames = _taskFunnel.GetChildPipeNames(taskExecutor.Key);
                if (childPipeNames?.Any() == true)
                {
                    sourcePipes[taskExecutor.Key] = childPipeNames;
                }
            }

            return sourcePipes;
        }
    }
}