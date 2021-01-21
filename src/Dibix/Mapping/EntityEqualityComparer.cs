using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityEqualityComparer<T> : EntityEqualityComparer, IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Create()
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(typeof(T));
            if (!entityDescriptor.Keys.Any())
                return EqualityComparer<T>.Default;

            return new EntityEqualityComparer<T>();
        }

        bool IEqualityComparer<T>.Equals(T x, T y) => base.EqualsCore(x, y);

        public int GetHashCode(T obj) => base.GetHashCode(obj);
    }

    internal class EntityEqualityComparer : IEqualityComparer<object>
    {
        public static IEqualityComparer<object> Instance { get; } = new EntityEqualityComparer();

        public static bool Equal(object x, object y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            if (x is byte[] binaryX && y is byte[] binaryY)
                return Enumerable.SequenceEqual(binaryX, binaryY);

            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(x.GetType());
            if (!entityDescriptor.Keys.Any())
                return false;

            return entityDescriptor.Keys.All(a => Equals(a.GetValue(x), a.GetValue(y)));
        }

        bool IEqualityComparer<object>.Equals(object x, object y) => this.EqualsCore(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj) => this.GetHashCode(obj);

        protected bool EqualsCore(object x, object y) => Equal(x, y);

        protected int GetHashCode(object obj)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(obj.GetType());
            if (!entityDescriptor.Keys.Any())
                return obj.GetHashCode();

            int hashCode = entityDescriptor.Keys
                                           .Select(x => x.GetValue(obj)?.GetHashCode() ?? 0)
                                           .Aggregate((x, y) => x ^ y);
            return hashCode;
        }
    }
}