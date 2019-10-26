using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace MsgNThen.Adapter
{
    public class RequestCookiesFeature : IRequestCookiesFeature
    {
        private static readonly Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = (Func<IFeatureCollection, IHttpRequestFeature>)(f => (IHttpRequestFeature)null);
        private FeatureReferences<IHttpRequestFeature> _features;
        private StringValues _original;
        private IRequestCookieCollection _parsedValues;

        public RequestCookiesFeature(IRequestCookieCollection cookies)
        {
            if (cookies == null)
                throw new ArgumentNullException(nameof(cookies));
            this._parsedValues = cookies;
        }

        public RequestCookiesFeature(IFeatureCollection features)
        {
            if (features == null)
                throw new ArgumentNullException(nameof(features));
            this._features = new FeatureReferences<IHttpRequestFeature>(features);
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get
            {
                return this._features.Fetch<IHttpRequestFeature>(ref this._features.Cache, RequestCookiesFeature._nullRequestFeature);
            }
        }

        public IRequestCookieCollection Cookies
        {
            get
            {
                if (this._features.Collection == null)
                {
                    if (this._parsedValues == null)
                        this._parsedValues = (IRequestCookieCollection)RequestCookieCollection.Empty;
                    return this._parsedValues;
                }
                StringValues empty;
                if (!this.HttpRequestFeature.Headers.TryGetValue("Cookie", out empty))
                    empty = (StringValues)string.Empty;
                if (this._parsedValues == null || this._original != empty)
                {
                    this._original = empty;
                    this._parsedValues = (IRequestCookieCollection)RequestCookieCollection.Parse((IList<string>)empty.ToArray());
                }
                return this._parsedValues;
            }
            set
            {
                this._parsedValues = value;
                this._original = StringValues.Empty;
                if (this._features.Collection == null)
                    return;
                if (this._parsedValues == null || this._parsedValues.Count == 0)
                {
                    ((IDictionary<string, StringValues>)this.HttpRequestFeature.Headers).Remove("Cookie");
                }
                else
                {
                    List<string> stringList = new List<string>();
                    foreach (KeyValuePair<string, string> parsedValue in (IEnumerable<KeyValuePair<string, string>>)this._parsedValues)
                        stringList.Add(new CookieHeaderValue((StringSegment)parsedValue.Key, (StringSegment)parsedValue.Value).ToString());
                    this._original = (StringValues)stringList.ToArray();
                    this.HttpRequestFeature.Headers["Cookie"] = this._original;
                }
            }
        }
    }
}