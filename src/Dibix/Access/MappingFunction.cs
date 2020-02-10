using System;
using System.Linq;

namespace Dibix
{
    public static class MappingFunction
    {
        public static void Deverticalize(object a, object b)
        {
            EntityDescriptor rootDescriptor = EntityDescriptorCache.GetDescriptor(a.GetType());
            if (!new PropertyMatcher().TryMatchProperty(rootDescriptor, typeof(object), out EntityProperty targetProperty)) 
                return;

            EntityDescriptor eavDescriptor = EntityDescriptorCache.GetDescriptor(b.GetType());

            object value = eavDescriptor.Properties
                                        .Where(x => x.Name.StartsWith("value_", StringComparison.OrdinalIgnoreCase))
                                        .Select(x => x.GetValue(b))
                                        .FirstOrDefault(x => x != null);

            targetProperty.SetValue(a, value);
        }
    }
}
