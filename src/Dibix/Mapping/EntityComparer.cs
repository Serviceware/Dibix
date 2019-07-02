using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(x.GetType());
            return entityDescriptor.Keys.All(a => Equals(a.GetValue(x), a.GetValue(y)));
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(obj.GetType());
            if (!entityDescriptor.Keys.Any())
                return obj.GetHashCode();

            int hashCode = entityDescriptor.Keys
                                           .Select(x => x.GetValue(obj).GetHashCode())
                                           .Aggregate((x, y) => x ^ y);
            return hashCode;
        }
    }

    internal sealed class EntityComparer<T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<object> _inner;

        public EntityComparer() => this._inner = new EntityComparer();

        bool IEqualityComparer<T>.Equals(T x, T y) => this._inner.Equals(x, y);

        int IEqualityComparer<T>.GetHashCode(T obj) => this._inner.GetHashCode(obj);
    }
}