using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class HttpRequestFeature : IHttpRequestFeature
    {
        public HttpRequestFeature()
        {
            this.Headers = (IHeaderDictionary)new HeaderDictionary();
            this.Body = Stream.Null;
            this.Protocol = string.Empty;
            this.Scheme = string.Empty;
            this.Method = string.Empty;
            this.PathBase = string.Empty;
            this.Path = string.Empty;
            this.QueryString = string.Empty;
            this.RawTarget = string.Empty;
        }

        public string Protocol { get; set; }

        public string Scheme { get; set; }

        public string Method { get; set; }

        public string PathBase { get; set; }

        public string Path { get; set; }

        public string QueryString { get; set; }

        public string RawTarget { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }
    }
}