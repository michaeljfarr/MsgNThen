using System;

namespace MsgNThen.Redis
{
    public class PipeInfo{
        public string ParentPipeName { get; }
        public string ChildPipeName { get; }
        private readonly string _suffix;

        private PipeInfo(string parentPipeName, string childPipeName)
        {
            ParentPipeName = parentPipeName;
            ChildPipeName = childPipeName;
            _suffix = $"{parentPipeName}{RedisTaskMultiplexorConstants.PathSeparator}{childPipeName}";
        }
        public string LockPath =>$"{RedisTaskMultiplexorConstants.RedisTaskMultiplexorPipePrefix}{_suffix}";
        public string PipePath => $"{RedisTaskMultiplexorConstants.RedisTaskMultiplexorLockPrefix}{_suffix}";
        public string BroadcastPath => RedisTaskMultiplexorConstants.RedisTaskMultiplexorBroadcastPrefix;

        public static PipeInfo Create(string parentPipeName, string childPipeName)
        {
            if (parentPipeName?.Contains(RedisTaskMultiplexorConstants.PathSeparator) == true || childPipeName?.Contains(RedisTaskMultiplexorConstants.PathSeparator) == true)
            {
                throw new ApplicationException($"Pipenames must not contain '{RedisTaskMultiplexorConstants.PathSeparator}' but names were '{parentPipeName}' and '{childPipeName}'");
            }
            return new PipeInfo(parentPipeName, childPipeName);
        }
    }
}