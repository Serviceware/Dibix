using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dibix.Generators
{
    // See:
    // https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#4-watch-out-for-collection-types
    // https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#pipeline-model-design
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>
    {
        private readonly T[]? _array;

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AsImmutableArray().ItemRef(index);
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsImmutableArray().IsEmpty;
        }

        public EquatableArray(ImmutableArray<T> array)
        {
            _array = Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
        }

        /// <sinheritdoc/>
        public bool Equals(EquatableArray<T> array)
        {
            return AsSpan().SequenceEqual(array.AsSpan());
        }

        /// <sinheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is EquatableArray<T> array && Equals(array);
        }

        /// <sinheritdoc/>
        public override int GetHashCode()
        {
            if (_array is not { } array)
            {
                return 0;
            }

            HashCode hashCode = default;

            foreach (T item in array)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImmutableArray<T> AsImmutableArray()
        {
            return Unsafe.As<T[]?, ImmutableArray<T>>(ref Unsafe.AsRef(in _array));
        }

        public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
        {
            return new EquatableArray<T>(array);
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return AsImmutableArray().AsSpan();
        }


        public T[] ToArray()
        {
            return AsImmutableArray().ToArray();
        }

        public ImmutableArray<T>.Enumerator GetEnumerator()
        {
            return AsImmutableArray().GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
        }

        /// <sinheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)AsImmutableArray()).GetEnumerator();
        }

        public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
        {
            return FromImmutableArray(array);
        }

        public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
        {
            return array.AsImmutableArray();
        }

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
        {
            return !left.Equals(right);
        }
    }

    internal static class EquatableArray
    {
        public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array) where T : IEquatable<T> => new EquatableArray<T>(array);
    }
}