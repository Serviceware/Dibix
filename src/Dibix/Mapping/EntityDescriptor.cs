using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    internal sealed class EntityDescriptor
    {
        private Delegate _postProcessor;

        public bool IsPrimitive { get; }
        public ICollection<EntityKey> Keys { get; }
        public EntityKey Discriminator { get; set; }
        public IList<EntityProperty> Properties { get; }

        public EntityDescriptor(bool isPrimitive)
        {
            this.IsPrimitive = isPrimitive;
            this.Keys = new Collection<EntityKey>();
            this.Properties = new Collection<EntityProperty>();
        }

        public void PostProcess(object instance) => this._postProcessor?.DynamicInvoke(instance);

        public void InitPostProcessor(Delegate postProcessor) => this._postProcessor = postProcessor;
    }
}