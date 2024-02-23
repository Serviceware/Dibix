using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class AssemblyHttpApiDiscoveryStrategy : IHttpApiDiscoveryStrategy
    {
        IEnumerable<HttpApiDescriptor> IHttpApiDiscoveryStrategy.Collect(IHttpApiDiscoveryContext context)
        {
            foreach (HttpApiDescriptor descriptor in CollectApiDescriptors())
            {
                descriptor.Configure(context);
                yield return descriptor;
            }
        }

        protected abstract IEnumerable<HttpApiDescriptor> CollectApiDescriptors();

        protected static HttpApiDescriptor CollectApiDescriptor(Assembly assembly)
        {
            Type baseType = typeof(HttpApiDescriptor);
            Type apiDescriptorType = assembly.GetLoadableTypes().FirstOrDefault(baseType.IsAssignableFrom);

            if (apiDescriptorType == null)
                throw new InvalidOperationException($"Could not find entry point inheriting from '{baseType}' in assembly: {assembly}");

            HttpApiDescriptor descriptor = (HttpApiDescriptor)Activator.CreateInstance(apiDescriptorType);
            return descriptor;
        }
    }
}