using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace MsgNThen.Adapter
{
    /// <summary>
    /// Represents a wrapper for RequestHeaders and ResponseHeaders.
    /// </summary>
    public class HeaderDictionary : IHeaderDictionary, IDictionary<string, StringValues>, ICollection<KeyValuePair<string, StringValues>>, IEnumerable<KeyValuePair<string, StringValues>>, IEnumerable
    {
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly StringValues[] EmptyValues = Array.Empty<StringValues>();
        private static readonly HeaderDictionary.Enumerator EmptyEnumerator = new HeaderDictionary.Enumerator();
        private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = (IEnumerator<KeyValuePair<string, StringValues>>)HeaderDictionary.EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = (IEnumerator)HeaderDictionary.EmptyEnumerator;

        public HeaderDictionary()
        {
        }

        public HeaderDictionary(Dictionary<string, StringValues> store)
        {
            this.Store = store;
        }

        public HeaderDictionary(int capacity)
        {
            this.EnsureStore(capacity);
        }

        private Dictionary<string, StringValues> Store { get; set; }

        private void EnsureStore(int capacity)
        {
            if (this.Store != null)
                return;
            this.Store = new Dictionary<string, StringValues>(capacity, (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
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
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                this.ThrowIfReadOnly();
                if (StringValues.IsNullOrEmpty(value))
                {
                    this.Store?.Remove(key);
                }
                else
                {
                    this.EnsureStore(1);
                    this.Store[key] = value;
                }
            }
        }

        StringValues IDictionary<string, StringValues>.this[
            string key]
        {
            get
            {
                return this.Store[key];
            }
            set
            {
                this.ThrowIfReadOnly();
                this[key] = value;
            }
        }

        public long? ContentLength
        {
            get
            {
                StringValues stringValues = this["Content-Length"];
                long result;
                if (stringValues.Count == 1 && !string.IsNullOrEmpty(stringValues[0]) && HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(stringValues[0]).Trim(), out result))
                    return new long?(result);
                return new long?();
            }
            set
            {
                this.ThrowIfReadOnly();
                if (value.HasValue)
                    this["Content-Length"] = (StringValues)HeaderUtilities.FormatNonNegativeInt64(value.Value);
                else
                    this.Remove("Content-Length");
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" />.</returns>
        public int Count
        {
            get
            {
                Dictionary<string, StringValues> store = this.Store;
                if (store == null)
                    return 0;
                return store.Count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" /> is in read-only mode.
        /// </summary>
        /// <returns>true if the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" /> is in read-only mode; otherwise, false.</returns>
        public bool IsReadOnly { get; set; }

        public ICollection<string> Keys
        {
            get
            {
                if (this.Store == null)
                    return (ICollection<string>)HeaderDictionary.EmptyKeys;
                return (ICollection<string>)this.Store.Keys;
            }
        }

        public ICollection<StringValues> Values
        {
            get
            {
                if (this.Store == null)
                    return (ICollection<StringValues>)HeaderDictionary.EmptyValues;
                return (ICollection<StringValues>)this.Store.Values;
            }
        }

        public void Add(KeyValuePair<string, StringValues> item)
        {
            if (item.Key == null)
                throw new ArgumentNullException("The key is null");
            this.ThrowIfReadOnly();
            this.EnsureStore(1);
            this.Store.Add(item.Key, item.Value);
        }

        /// <summary>Adds the given header and values to the collection.</summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header values.</param>
        public void Add(string key, StringValues value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            this.ThrowIfReadOnly();
            this.EnsureStore(1);
            this.Store.Add(key, value);
        }

        /// <summary>Clears the entire list of objects.</summary>
        public void Clear()
        {
            this.ThrowIfReadOnly();
            this.Store?.Clear();
        }

        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            StringValues left;
            return this.Store != null && this.Store.TryGetValue(item.Key, out left) && StringValues.Equals(left, item.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            if (this.Store == null)
                return false;
            return this.Store.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            if (this.Store == null)
                return;
            foreach (KeyValuePair<string, StringValues> keyValuePair in this.Store)
            {
                array[arrayIndex] = keyValuePair;
                ++arrayIndex;
            }
        }

        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            this.ThrowIfReadOnly();
            StringValues right;
            if (this.Store == null || !this.Store.TryGetValue(item.Key, out right) || !StringValues.Equals(item.Value, right))
                return false;
            return this.Store.Remove(item.Key);
        }

        /// <summary>Removes the given header from the collection.</summary>
        /// <param name="key">The header name.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(string key)
        {
            this.ThrowIfReadOnly();
            if (this.Store == null)
                return false;
            return this.Store.Remove(key);
        }

        /// <summary>Retrieves a value from the dictionary.</summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary" /> contains the key; otherwise, false.</returns>
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
        /// <returns>An <see cref="T:Microsoft.AspNetCore.Http.HeaderDictionary.Enumerator" /> object that can be used to iterate through the collection.</returns>
        public HeaderDictionary.Enumerator GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return HeaderDictionary.EmptyEnumerator;
            return new HeaderDictionary.Enumerator(this.Store.GetEnumerator());
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return HeaderDictionary.EmptyIEnumeratorType;
            return (IEnumerator<KeyValuePair<string, StringValues>>)this.Store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return HeaderDictionary.EmptyIEnumerator;
            return (IEnumerator)this.Store.GetEnumerator();
        }

        private void ThrowIfReadOnly()
        {
            if (this.IsReadOnly)
                throw new InvalidOperationException("The response headers cannot be modified because the response has already started.");
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