using System;
using System.Collections;
using System.Collections.Generic;

namespace MsgNThen.Adapter
{
    internal class CopyOnWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        private readonly IDictionary<TKey, TValue> _sourceDictionary;
        private readonly IEqualityComparer<TKey> _comparer;
        private IDictionary<TKey, TValue> _innerDictionary;

        public CopyOnWriteDictionary(
            IDictionary<TKey, TValue> sourceDictionary,
            IEqualityComparer<TKey> comparer)
        {
            if (sourceDictionary == null)
                throw new ArgumentNullException(nameof(sourceDictionary));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            this._sourceDictionary = sourceDictionary;
            this._comparer = comparer;
        }

        private IDictionary<TKey, TValue> ReadDictionary
        {
            get
            {
                return this._innerDictionary ?? this._sourceDictionary;
            }
        }

        private IDictionary<TKey, TValue> WriteDictionary
        {
            get
            {
                if (this._innerDictionary == null)
                    this._innerDictionary = (IDictionary<TKey, TValue>)new Dictionary<TKey, TValue>(this._sourceDictionary, this._comparer);
                return this._innerDictionary;
            }
        }

        public virtual ICollection<TKey> Keys
        {
            get
            {
                return this.ReadDictionary.Keys;
            }
        }

        public virtual ICollection<TValue> Values
        {
            get
            {
                return this.ReadDictionary.Values;
            }
        }

        public virtual int Count
        {
            get
            {
                return this.ReadDictionary.Count;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                return this.ReadDictionary[key];
            }
            set
            {
                this.WriteDictionary[key] = value;
            }
        }

        public virtual bool ContainsKey(TKey key)
        {
            return this.ReadDictionary.ContainsKey(key);
        }

        public virtual void Add(TKey key, TValue value)
        {
            this.WriteDictionary.Add(key, value);
        }

        public virtual bool Remove(TKey key)
        {
            return this.WriteDictionary.Remove(key);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return this.ReadDictionary.TryGetValue(key, out value);
        }

        public virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            this.WriteDictionary.Add(item);
        }

        public virtual void Clear()
        {
            this.WriteDictionary.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.ReadDictionary.Contains(item);
        }

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.ReadDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)this.WriteDictionary).Remove(item);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.ReadDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }
    }
}