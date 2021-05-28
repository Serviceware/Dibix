using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    public sealed class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;
        private readonly Func<T, int> _hash;

        private FuncEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash)
        {
            this._comparer = comparer;
            this._hash = hash;
        }

        public static IEqualityComparer<T> Create(Func<T, T, bool> comparer, Func<T, int> hash)
        {
            return new FuncEqualityComparer<T>(comparer, hash);
        }
        public static IEqualityComparer<T> Create<TKey>(Func<T, TKey> keySelector)
        {
            return new FuncEqualityComparer<T>((x, y) => AreEqual(x, y, keySelector), x => GetHashCode(x, keySelector));
        }
        public static IEqualityComparer<T> Create<TKey1, TKey2>(Func<T, TKey1> firstKeySelector, Func<T, TKey2> secondKeySelector)
        {
            return new FuncEqualityComparer<T>((x, y) => AreEqual(x, y, firstKeySelector, secondKeySelector), x => GetHashCode(x, firstKeySelector, secondKeySelector));
        }

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            return this._comparer(x, y);
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return this._hash(obj);
        }

        private static int GetHashCode<TKey>(T x, Func<T, TKey> keySelector)
        {
            TKey value = keySelector(x);
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
                return enumerable.Cast<object>().Count().GetHashCode();

            return value.GetHashCode();
        }
        private static int GetHashCode<TKey1, TKey2>(T x, Func<T, TKey1> firstKeySelector, Func<T, TKey2> secondKeySelector)
        {
            unchecked
            {
                return (GetHashCode(x, firstKeySelector) * 397) ^ GetHashCode(x, secondKeySelector);
            }
        }

        private static bool AreEqual<TKey>(T a, T b, Func<T, TKey> keySelector)
        {
            TKey valueA = keySelector(a);
            TKey valueB = keySelector(b);
            IEnumerable enumerableA = valueA as IEnumerable;
            IEnumerable enumerableB = valueB as IEnumerable;
            if (enumerableA != null && enumerableB != null)
                return Enumerable.SequenceEqual(enumerableA.Cast<object>(), enumerableB.Cast<object>());

            return Equals(valueA, valueB);
        }
        private static bool AreEqual<TKey1, TKey2>(T a, T b, Func<T, TKey1> firstKeySelector, Func<T, TKey2> secondKeySelector) => AreEqual(a, b, firstKeySelector) && AreEqual(a, b, secondKeySelector);
    }
}