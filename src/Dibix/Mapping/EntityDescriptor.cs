using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    internal sealed class EntityDescriptor
    {
        public ICollection<EntityKey> Keys { get; }
        public EntityKey Discriminator { get; set; }
        public IList<EntityProperty> ComplexProperties { get; }
        public ICollection<ObfuscatedProperty> ObfuscatedProperties { get; }

        public EntityDescriptor()
        {
            this.Keys = new Collection<EntityKey>();
            this.ComplexProperties = new Collection<EntityProperty>();
            this.ObfuscatedProperties = new Collection<ObfuscatedProperty>();
        }
    }
}