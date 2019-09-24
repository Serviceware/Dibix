using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class RecursiveMapper : IPostProcessor
    {
        public IEnumerable<TReturn> PostProcess<TReturn>(IEnumerable<TReturn> source)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(typeof(TReturn));
            if (entityDescriptor.Discriminator == null)
                return source;

            ICollection<TReturn> resolved = source.ToArray();
            if (!new PropertyMatcher().TryMatchProperty(entityDescriptor, typeof(TReturn), out EntityProperty property))
                return resolved;

            // Map recursive relational model to a hierarchical tree model based on a 'ParentId' like discriminator
            EntityKey key = entityDescriptor.Keys.Single();
            IDictionary<object, TReturn> entityMap = resolved.ToDictionary(x => key.GetValue(x));
            ILookup<object, TReturn> childEntityMap = resolved.ToLookup(x => entityDescriptor.Discriminator.GetValue(x), x => entityMap[key.GetValue(x)]);

            foreach (TReturn entity in entityMap.Values)
            {
                foreach (TReturn childEntity in childEntityMap[key.GetValue(entity)])
                {
                    property.SetValue(entity, childEntity);
                }
            }

            return childEntityMap[null]; // Return the root collection
        }
    }
}