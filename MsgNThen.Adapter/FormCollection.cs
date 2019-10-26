using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MsgNThen.Adapter
{
    /// <summary>Contains the parsed form values.</summary>
    public class FormCollection : IFormCollection, IEnumerable<KeyValuePair<string, StringValues>>, IEnumerable
    {
        public static readonly FormCollection Empty = new FormCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly StringValues[] EmptyValues = Array.Empty<StringValues>();
        private static readonly FormCollection.Enumerator EmptyEnumerator = new FormCollection.Enumerator();
        private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = (IEnumerator<KeyValuePair<string, StringValues>>)FormCollection.EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = (IEnumerator)FormCollection.EmptyEnumerator;
        private static IFormFileCollection EmptyFiles = (IFormFileCollection)new FormFileCollection();
        private IFormFileCollection _files;

        private FormCollection()
        {
        }

        public FormCollection(Dictionary<string, StringValues> fields, IFormFileCollection files = null)
        {
            this.Store = fields;
            this._files = files;
        }

        public IFormFileCollection Files
        {
            get
            {
                return this._files ?? FormCollection.EmptyFiles;
            }
            private set
            {
                this._files = value;
            }
        }

        private Dictionary<string, StringValues> Store { get; set; }

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

        public ICollection<string> Keys
        {
            get
            {
                if (this.Store == null)
                    return (ICollection<string>)FormCollection.EmptyKeys;
                return (ICollection<string>)this.Store.Keys;
            }
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
        /// Returns an struct enumerator that iterates through a collection without boxing and is also used via the <see cref="T:Microsoft.AspNetCore.Http.IFormCollection" /> interface.
        /// </summary>
        /// <returns>An <see cref="T:Microsoft.AspNetCore.Http.FormCollection.Enumerator" /> object that can be used to iterate through the collection.</returns>
        public FormCollection.Enumerator GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return FormCollection.EmptyEnumerator;
            return new FormCollection.Enumerator(this.Store.GetEnumerator());
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return FormCollection.EmptyIEnumeratorType;
            return (IEnumerator<KeyValuePair<string, StringValues>>)this.Store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Store == null || this.Store.Count == 0)
                return FormCollection.EmptyIEnumerator;
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