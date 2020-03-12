using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class RecursiveMapper : IPostProcessor
    {
        public IEnumerable<object> PostProcess(IEnumerable<object> source, Type type)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(type);
            if (entityDescriptor.Discriminator == null)
                return source;

            ICollection<object> resolved = source.ToArray();
            if (!new PropertyMatcher().TryMatchProperty(entityDescriptor, type, out EntityProperty property))
                return resolved;

            // Map recursive relational model to a hierarchical tree model based on a 'ParentId' like discriminator
            EntityKey key = entityDescriptor.Keys.Single();
            IDictionary<object, object> entityMap = resolved.ToDictionary(x => key.GetValue(x));
            ILookup<object, object> childEntityMap = resolved.ToLookup(x => entityDescriptor.Discriminator.GetValue(x), x => entityMap[key.GetValue(x)]);

            foreach (object entity in entityMap.Values)
            {
                foreach (object childEntity in childEntityMap[key.GetValue(entity)])
                {
                    property.SetValue(entity, childEntity);
                }
            }

            return childEntityMap[null]; // Return the root collection
        }
    }
}