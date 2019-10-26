using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class HttpResponseFeature : IHttpResponseFeature
    {
        public HttpResponseFeature()
        {
            this.StatusCode = 200;
            this.Headers = (IHeaderDictionary)new HeaderDictionary();
            this.Body = Stream.Null;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

        public virtual bool HasStarted
        {
            get
            {
                return false;
            }
        }

        public virtual void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public virtual void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}