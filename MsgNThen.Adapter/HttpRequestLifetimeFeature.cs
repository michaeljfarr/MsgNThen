using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; }

        public void Abort()
        {
        }
    }
}