using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Adapter
{

    public class DefaultHttpResponse : HttpResponse
    {
        private static readonly Func<IFeatureCollection, IHttpResponseFeature> _nullResponseFeature = (Func<IFeatureCollection, IHttpResponseFeature>)(f => (IHttpResponseFeature)null);
        private HttpContext _context;
        private FeatureReferences<DefaultHttpResponse.FeatureInterfaces> _features;

        public DefaultHttpResponse(HttpContext context)
        {
            this.Initialize(context);
        }

        public virtual void Initialize(HttpContext context)
        {
            this._context = context;
            this._features = new FeatureReferences<DefaultHttpResponse.FeatureInterfaces>(context.Features);
        }

        public virtual void Uninitialize()
        {
            this._context = (HttpContext)null;
            this._features = new FeatureReferences<DefaultHttpResponse.FeatureInterfaces>();
        }

        private IHttpResponseFeature HttpResponseFeature
        {
            get
            {
                return this._features.Fetch<IHttpResponseFeature>(ref this._features.Cache.Response, DefaultHttpResponse._nullResponseFeature);
            }
        }

        public override HttpContext HttpContext
        {
            get
            {
                return this._context;
            }
        }

        public override int StatusCode
        {
            get
            {
                return this.HttpResponseFeature.StatusCode;
            }
            set
            {
                this.HttpResponseFeature.StatusCode = value;
            }
        }

        public override IHeaderDictionary Headers
        {
            get
            {
                return this.HttpResponseFeature.Headers;
            }
        }

        public override Stream Body
        {
            get
            {
                return this.HttpResponseFeature.Body;
            }
            set
            {
                this.HttpResponseFeature.Body = value;
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

        public override string ContentType
        {
            get
            {
                return (string)this.Headers["Content-Type"];
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    ((IDictionary<string, StringValues>)this.HttpResponseFeature.Headers).Remove("Content-Type");
                else
                    this.HttpResponseFeature.Headers["Content-Type"] = (StringValues)value;
            }
        }

        public override IResponseCookies Cookies
        {
            get
            {
                return null;
            }
        }

        public override bool HasStarted
        {
            get
            {
                return this.HttpResponseFeature.HasStarted;
            }
        }

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            this.HttpResponseFeature.OnStarting(callback, state);
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            this.HttpResponseFeature.OnCompleted(callback, state);
        }

        public override void Redirect(string location, bool permanent)
        {
            this.HttpResponseFeature.StatusCode = !permanent ? 302 : 301;
            this.Headers["Location"] = (StringValues)location;
        }

        private struct FeatureInterfaces
        {
            public IHttpResponseFeature Response;
            public IResponseCookiesFeature Cookies;
        }
    }
}