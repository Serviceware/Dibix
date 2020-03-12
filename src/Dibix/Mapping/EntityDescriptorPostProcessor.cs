using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityDescriptorPostProcessor : IPostProcessor
    {
        public IEnumerable<object> PostProcess(IEnumerable<object> source, Type type)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(type);
            return source.Select(x =>
            {
                entityDescriptor.PostProcess(x);
                return x;
            });
        }
    }
}