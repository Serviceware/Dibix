using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix
{
    internal sealed class PropertyMatcher
    {
        private readonly ICollection<EntityProperty> _matchedProperties;

        public PropertyMatcher()
        {
            this._matchedProperties = new Collection<EntityProperty>();
        }

        public bool TryMatchProperty(EntityDescriptor descriptor, Type sourceType, out EntityProperty property)
        {
            property = descriptor.Properties
                                 .Reverse()
                                 .FirstOrDefault(x => x.EntityType == sourceType && !this._matchedProperties.Contains(x) /* Skip properties that have already been matched */);
                                                                                                                         /* i.E.: multiple properties of the same type     */

            if (property == null)
                return false;

            this._matchedProperties.Add(property);
            return true;
        }
    }
}