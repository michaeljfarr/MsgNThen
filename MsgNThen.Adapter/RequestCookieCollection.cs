using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MsgNThen.Adapter
{
    public class RequestCookieCollection : IRequestCookieCollection, IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        public static readonly RequestCookieCollection Empty = new RequestCookieCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly RequestCookieCollection.Enumerator EmptyEnumerator = new RequestCookieCollection.Enumerator();
        private static readonly IEnumerator<KeyValuePair<string, string>> EmptyIEnumeratorType = (IEnumerator<KeyValuePair<string, string>>)RequestCookieCollection.EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = (IEnumerator)RequestCookieCollection.EmptyEnumerator;

        private Dictionary<string, string> Store { get; set; }

        public RequestCookieCollection()
        {
        }

        public RequestCookieCollection(Dictionary<string, string> store)
        {
            this.Store = store;
        }

        public RequestCookieCollection(int capacity)
        {
            this.Store = new Dictionary<string, string>(capacity, (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        }

        public string this[string key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (this.Store == null)
                    return (string)null;
                string str;
                if (this.TryGetValue(key, out str))
                    return str;
                return (string)null;
            }
        }

        public static RequestCookieCollection Parse(IList<string> values)
        {
            IList<CookieHeaderValue> parsedValues;
            if (values.Count == 0 || !CookieHeaderValue.TryParseList(values, out parsedValues) || parsedValues.Count == 0)
                return RequestCookieCollection.Empty;
            RequestCookieCollection cookieCollection = new RequestCookieCollection(parsedValues.Count);
            Dictionary<string, string> store = cookieCollection.Store;
            for (int index1 = 0; index1 < parsedValues.Count; ++index1)
            {
                CookieHeaderValue cookieHeaderValue = parsedValues[index1];
                string index2 = Uri.UnescapeDataString(cookieHeaderValue.Name.Value);
                string str = Uri.UnescapeDataString(cookieHeaderValue.Value.Value);
                store[index2] = str;
            }
            return cookieCollection;
        }

        public int Count
        {
            get
            {
                if (this.Store == null)
                    return 0;
                return this.Store.Count;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (this.Store == null)
                    return (ICollection<string>)RequestCookieCollection.EmptyKeys;
                return (ICollection<string>)this.Store.Keys;
            }
        }

        public bool ContainsKey(string key)
        {
            if (this.Store == null)
                return false;
            return this.Store.ContainsKey(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            if (this.Store != null)
                return this.Store.TryGetValue(key, out value);
            value = (string)null;
            return false;
        }

        /// <summary>
        /// Returns an struct enumerator that iterates through a collection without boxing.
        /// </summary>
        /// <returns>An <see cref="T:Microsoft.AspNetCore.Http.Internal.RequestCookieCollection.Enumerator" /> object that can be used to iterate through the collection.</returns>
        public RequestCookieCollection.Enumerator GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return RequestCookieCollection.EmptyEnumerator;
            return new RequestCookieCollection.Enumerator(this.Store.GetEnumerator());
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return RequestCookieCollection.EmptyIEnumeratorType;
            return (IEnumerator<KeyValuePair<string, string>>)this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return RequestCookieCollection.EmptyIEnumerator;
            return (IEnumerator)this.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, string>>, IEnumerator, IDisposable
        {
            private Dictionary<string, string>.Enumerator _dictionaryEnumerator;
            private bool _notEmpty;

            internal Enumerator(
                Dictionary<string, string>.Enumerator dictionaryEnumerator)
            {
                this._dictionaryEnumerator = dictionaryEnumerator;
                this._notEmpty = true;
            }

            public bool MoveNext()
            {
                if (this._notEmpty)
                    return this._dictionaryEnumerator.MoveNext();
                return false;
            }

            public KeyValuePair<string, string> Current
            {
                get
                {
                    if (!this._notEmpty)
                        return new KeyValuePair<string, string>();
                    KeyValuePair<string, string> current = this._dictionaryEnumerator.Current;
                    return new KeyValuePair<string, string>(current.Key, current.Value);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return (object)this.Current;
                }
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                if (!this._notEmpty)
                    return;
                ((IEnumerator)this._dictionaryEnumerator).Reset();
            }
        }
    }
}