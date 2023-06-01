using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class EntityDescriptorPostProcessor : IPostProcessor
    {
        public IEnumerable<T> PostProcess<T>(IEnumerable<T> source, Type type)
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