using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityEqualityComparer<T> : IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Instance { get; } = new EntityEqualityComparer<T>();

        public static bool Equal(T x, T y)
        {
            if (object.Equals(x, y))
                return true;
            
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

            EntityDescriptor entityDescriptorX = EntityDescriptorCache.GetDescriptor(x.GetType());
            EntityDescriptor entityDescriptorY = EntityDescriptorCache.GetDescriptor(y.GetType());

            if (entityDescriptorX.IsPrimitive && entityDescriptorY.IsPrimitive)
                return Object.Equals(x, y);

            if (!entityDescriptorX.Keys.Any())
                return false;

            return entityDescriptorX.Keys.All(a => Equals(a.GetValue(x), a.GetValue(y)));
        }

        bool IEqualityComparer<T>.Equals(T x, T y) => Equals(x, y);

        int IEqualityComparer<T>.GetHashCode(T obj) => GetHashCode(obj);

        private static bool Equals(T x, T y) => Equal(x, y);

        private static int GetHashCode(T obj)
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