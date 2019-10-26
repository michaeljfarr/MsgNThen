using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Adapter
{
    public class QueryFeature : IQueryFeature
    {
        private static readonly Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = (Func<IFeatureCollection, IHttpRequestFeature>)(f => (IHttpRequestFeature)null);
        private FeatureReferences<IHttpRequestFeature> _features;
        private string _original;
        private IQueryCollection _parsedValues;

        public QueryFeature(IQueryCollection query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            this._parsedValues = query;
        }

        public QueryFeature(IFeatureCollection features)
        {
            if (features == null)
                throw new ArgumentNullException(nameof(features));
            this._features = new FeatureReferences<IHttpRequestFeature>(features);
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get
            {
                return this._features.Fetch<IHttpRequestFeature>(ref this._features.Cache, QueryFeature._nullRequestFeature);
            }
        }

        public IQueryCollection Query
        {
            get
            {
                if (this._features.Collection == null)
                {
                    if (this._parsedValues == null)
                        this._parsedValues = (IQueryCollection)QueryCollection.Empty;
                    return this._parsedValues;
                }
                string queryString = this.HttpRequestFeature.QueryString;
                if (this._parsedValues == null || !string.Equals(this._original, queryString, StringComparison.Ordinal))
                {
                    this._original = queryString;
                    Dictionary<string, StringValues> nullableQuery = QueryHelpers.ParseNullableQuery(queryString);
                    this._parsedValues = nullableQuery != null ? (IQueryCollection)new QueryCollection(nullableQuery) : (IQueryCollection)QueryCollection.Empty;
                }
                return this._parsedValues;
            }
            set
            {
                this._parsedValues = value;
                if (this._features.Collection == null)
                    return;
                if (value == null)
                {
                    this._original = string.Empty;
                    this.HttpRequestFeature.QueryString = string.Empty;
                }
                else
                {
                    this._original = QueryString.Create((IEnumerable<KeyValuePair<string, StringValues>>)this._parsedValues).ToString();
                    this.HttpRequestFeature.QueryString = this._original;
                }
            }
        }
    }
}