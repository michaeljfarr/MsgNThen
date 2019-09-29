using System;
using System.Threading;

namespace MsgNThen.Redis
{
    public interface ITaskReader
    {
        void Start(TimeSpan lockExpiry, CancellationToken cancellationToken);
    }
}