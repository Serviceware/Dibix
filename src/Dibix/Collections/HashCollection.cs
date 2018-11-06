using System.Collections;
using System.Collections.Generic;

namespace Dibix
{
    // Unfortunately HashSet<T> does not support retrieval of items in .NET Standard 2.0
    // It was only introduced in .NET Core 2.0/.NET Framework 4.7.2
    // Therefore we use a dictionary with key = value
    public sealed class HashCollection<T> : ICollection<T>, IEnumerable<T>
    {
        #region Fields
        private readonly IDictionary<T, T> _dictionary;
        #endregion

        #region Constructor
        public HashCollection() : this(null) { }
        public HashCollection(IEqualityComparer<T> comparer)
        {
            this._dictionary = comparer != null ? new Dictionary<T, T>(comparer) : new Dictionary<T, T>();
        }
        #endregion

        #region Public Methods

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            return this._dictionary.TryGetValue(equalValue, out actualValue);
        }
        #endregion

        #region ICollection<T> Members
        public int Count => this._dictionary.Keys.Count;
        public bool IsReadOnly => this._dictionary.Keys.IsReadOnly;
        public void Add(T item)
        {
            if (this._dictionary.ContainsKey(item))
                return;

            this._dictionary.Add(item, item);
        }

        public bool Contains(T item)
        {
            return this._dictionary.ContainsKey(item);
        }

        public void Clear()
        {
            this._dictionary.Keys.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this._dictionary.Keys.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return this._dictionary.Keys.Remove(item);
        }
        #endregion

        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            return this._dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }
}