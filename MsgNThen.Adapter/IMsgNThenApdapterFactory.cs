using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;

namespace MsgNThen.Adapter
{
    public interface IMsgNThenApdapterFactory
    {
        IMsgNThenHttpAdapter Start<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken);
    }
}