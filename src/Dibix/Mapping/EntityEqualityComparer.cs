using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly ICollection<EntityKey> _keys;

        private EntityEqualityComparer(ICollection<EntityKey> keys)
        {
            this._keys = keys;
        }

        public static IEqualityComparer<T> Create()
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(typeof(T));
            if (!entityDescriptor.Keys.Any())
                return EqualityComparer<T>.Default;

            return new EntityEqualityComparer<T>(entityDescriptor.Keys);
        }

        public bool Equals(T x, T y)
        {
            if (ReferenceEquals(x, y)) 
                return true;

            if (ReferenceEquals(x, null)) 
                return false;

            if (ReferenceEquals(y, null)) 
                return false;

            if (x.GetType() != y.GetType()) 
                return false;

            return this._keys.All(key => Equals(key.GetValue(x), key.GetValue(y)));
        }

        public int GetHashCode(T obj)
        {
            int hashCode = this._keys
                               .Select(x => x.GetValue(obj).GetHashCode())
                               .Aggregate((x, y) => (x * 397) ^ y);
            return hashCode;
        }
    }
}