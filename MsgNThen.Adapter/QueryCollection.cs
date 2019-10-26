using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Adapter
{
    public class QueryCollection : IQueryCollection, IEnumerable<KeyValuePair<string, StringValues>>, IEnumerable
    {
        public static readonly QueryCollection Empty = new QueryCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly StringValues[] EmptyValues = Array.Empty<StringValues>();
        private static readonly QueryCollection.Enumerator EmptyEnumerator = new QueryCollection.Enumerator();
        private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = (IEnumerator<KeyValuePair<string, StringValues>>)QueryCollection.EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = (IEnumerator)QueryCollection.EmptyEnumerator;

        private Dictionary<string, StringValues> Store { get; set; }

        public QueryCollection()
        {
        }

        public QueryCollection(Dictionary<string, StringValues> store)
        {
            this.Store = store;
        }

        public QueryCollection(QueryCollection store)
        {
            this.Store = store.Store;
        }

        public QueryCollection(int capacity)
        {
            this.Store = new Dictionary<string, StringValues>(capacity, (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns>the associated value from the collection as a StringValues or StringValues.Empty if the key is not present.</returns>
        public StringValues this[string key]
        {
            get
            {
                StringValues stringValues;
                if (this.Store == null || !this.TryGetValue(key, out stringValues))
                    return StringValues.Empty;
                return stringValues;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection" />.</returns>
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
                    return (ICollection<string>)QueryCollection.EmptyKeys;
                return (ICollection<string>)this.Store.Keys;
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            if (this.Store == null)
                return false;
            return this.Store.ContainsKey(key);
        }

        /// <summary>Retrieves a value from the collection.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection" /> contains the key; otherwise, false.</returns>
        public bool TryGetValue(string key, out StringValues value)
        {
            if (this.Store != null)
                return this.Store.TryGetValue(key, out value);
            value = new StringValues();
            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:Microsoft.AspNetCore.Http.Internal.QueryCollection.Enumerator" /> object that can be used to iterate through the collection.</returns>
        public QueryCollection.Enumerator GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return QueryCollection.EmptyEnumerator;
            return new QueryCollection.Enumerator(this.Store.GetEnumerator());
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return QueryCollection.EmptyIEnumeratorType;
            return (IEnumerator<KeyValuePair<string, StringValues>>)this.Store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return QueryCollection.EmptyIEnumerator;
            return (IEnumerator)this.Store.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>, IEnumerator, IDisposable
        {
            private Dictionary<string, StringValues>.Enumerator _dictionaryEnumerator;
            private bool _notEmpty;

            internal Enumerator(
                Dictionary<string, StringValues>.Enumerator dictionaryEnumerator)
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

            public KeyValuePair<string, StringValues> Current
            {
                get
                {
                    if (this._notEmpty)
                        return this._dictionaryEnumerator.Current;
                    return new KeyValuePair<string, StringValues>();
                }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return (object)this.Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (!this._notEmpty)
                    return;
                ((IEnumerator)this._dictionaryEnumerator).Reset();
            }
        }
    }
}