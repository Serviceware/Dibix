using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    internal sealed class EntityDescriptor
    {
        public ICollection<EntityKey> Keys { get; }
        public IList<EntityProperty> ComplexProperties { get; }

        public EntityDescriptor()
        {
            this.Keys = new Collection<EntityKey>();
            this.ComplexProperties = new Collection<EntityProperty>();
        }
    }
}