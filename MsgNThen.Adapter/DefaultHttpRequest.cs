using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Adapter
{
    public class DefaultHttpRequest : HttpRequest
    {
        private static readonly Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = (Func<IFeatureCollection, IHttpRequestFeature>)(f => (IHttpRequestFeature)null);
        private static readonly Func<IFeatureCollection, IQueryFeature> _newQueryFeature = (Func<IFeatureCollection, IQueryFeature>)(f => (IQueryFeature)new QueryFeature(f));
        private static readonly Func<HttpRequest, IFormFeature> _newFormFeature = (Func<HttpRequest, IFormFeature>)(r => (IFormFeature)new FormFeature(r));
        private static readonly Func<IFeatureCollection, IRequestCookiesFeature> _newRequestCookiesFeature = (Func<IFeatureCollection, IRequestCookiesFeature>)(f => (IRequestCookiesFeature)new RequestCookiesFeature(f));
        private HttpContext _context;
        private FeatureReferences<DefaultHttpRequest.FeatureInterfaces> _features;

        public DefaultHttpRequest(HttpContext context)
        {
            this.Initialize(context);
        }

        public virtual void Initialize(HttpContext context)
        {
            this._context = context;
            this._features = new FeatureReferences<DefaultHttpRequest.FeatureInterfaces>(context.Features);
        }

        public virtual void Uninitialize()
        {
            this._context = (HttpContext)null;
            this._features = new FeatureReferences<DefaultHttpRequest.FeatureInterfaces>();
        }

        public override HttpContext HttpContext
        {
            get
            {
                return this._context;
            }
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get
            {
                return this._features.Fetch<IHttpRequestFeature>(ref this._features.Cache.Request, DefaultHttpRequest._nullRequestFeature);
            }
        }

        private IQueryFeature QueryFeature
        {
            get
            {
                return this._features.Fetch<IQueryFeature>(ref this._features.Cache.Query, DefaultHttpRequest._newQueryFeature);
            }
        }

        private IFormFeature FormFeature
        {
            get
            {
                return this._features.Fetch<IFormFeature, HttpRequest>(ref this._features.Cache.Form, (HttpRequest)this, DefaultHttpRequest._newFormFeature);
            }
        }

        private IRequestCookiesFeature RequestCookiesFeature
        {
            get
            {
                return this._features.Fetch<IRequestCookiesFeature>(ref this._features.Cache.Cookies, DefaultHttpRequest._newRequestCookiesFeature);
            }
        }

        public override PathString PathBase
        {
            get
            {
                return new PathString(this.HttpRequestFeature.PathBase);
            }
            set
            {
                this.HttpRequestFeature.PathBase = value.Value;
            }
        }

        public override PathString Path
        {
            get
            {
                return new PathString(this.HttpRequestFeature.Path);
            }
            set
            {
                this.HttpRequestFeature.Path = value.Value;
            }
        }

        public override QueryString QueryString
        {
            get
            {
                return new QueryString(this.HttpRequestFeature.QueryString);
            }
            set
            {
                this.HttpRequestFeature.QueryString = value.Value;
            }
        }

        public override long? ContentLength
        {
            get
            {
                return this.Headers.ContentLength;
            }
            set
            {
                this.Headers.ContentLength = value;
            }
        }

        public override Stream Body
        {
            get
            {
                return this.HttpRequestFeature.Body;
            }
            set
            {
                this.HttpRequestFeature.Body = value;
            }
        }

        public override string Method
        {
            get
            {
                return this.HttpRequestFeature.Method;
            }
            set
            {
                this.HttpRequestFeature.Method = value;
            }
        }

        public override string Scheme
        {
            get
            {
                return this.HttpRequestFeature.Scheme;
            }
            set
            {
                this.HttpRequestFeature.Scheme = value;
            }
        }

        public override bool IsHttps
        {
            get
            {
                return string.Equals("https", this.Scheme, StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                this.Scheme = value ? "https" : "http";
            }
        }

        public override HostString Host
        {
            get
            {
                return HostString.FromUriComponent((string)this.Headers[nameof(Host)]);
            }
            set
            {
                this.Headers[nameof(Host)] = (StringValues)value.ToUriComponent();
            }
        }

        public override IQueryCollection Query
        {
            get
            {
                return this.QueryFeature.Query;
            }
            set
            {
                this.QueryFeature.Query = value;
            }
        }

        public override string Protocol
        {
            get
            {
                return this.HttpRequestFeature.Protocol;
            }
            set
            {
                this.HttpRequestFeature.Protocol = value;
            }
        }

        public override IHeaderDictionary Headers
        {
            get
            {
                return this.HttpRequestFeature.Headers;
            }
        }

        public override IRequestCookieCollection Cookies
        {
            get
            {
                return this.RequestCookiesFeature.Cookies;
            }
            set
            {
                this.RequestCookiesFeature.Cookies = value;
            }
        }

        public override string ContentType
        {
            get
            {
                return (string)this.Headers["Content-Type"];
            }
            set
            {
                this.Headers["Content-Type"] = (StringValues)value;
            }
        }

        public override bool HasFormContentType
        {
            get
            {
                return this.FormFeature.HasFormContentType;
            }
        }

        public override IFormCollection Form
        {
            get
            {
                return this.FormFeature.ReadForm();
            }
            set
            {
                this.FormFeature.Form = value;
            }
        }

        public override Task<IFormCollection> ReadFormAsync(
            CancellationToken cancellationToken)
        {
            return this.FormFeature.ReadFormAsync(cancellationToken);
        }

        private struct FeatureInterfaces
        {
            public IHttpRequestFeature Request;
            public IQueryFeature Query;
            public IFormFeature Form;
            public IRequestCookiesFeature Cookies;
        }
    }
}