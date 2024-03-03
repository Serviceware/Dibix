using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class StaticAssemblyHttpApiDiscoveryStrategy : AssemblyHttpApiDiscoveryStrategy, IHttpApiDiscoveryStrategy
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public StaticAssemblyHttpApiDiscoveryStrategy(IEnumerable<Assembly> assemblies) => _assemblies = assemblies;

        protected override IEnumerable<HttpApiDescriptor> CollectApiDescriptors()
        {
            foreach (Assembly assembly in _assemblies)
            {
                HttpApiDescriptor descriptor = CollectApiDescriptor(assembly);
                descriptor.Metadata.AreaName = ResolveAreaName(assembly);
                yield return descriptor;
            }
        }

        private static string ResolveAreaName(Assembly assembly)
        {
            Type areaRegistrationAttribute = typeof(AreaRegistrationAttribute);
            AreaRegistrationAttribute attribute = assembly.GetCustomAttribute(areaRegistrationAttribute) as AreaRegistrationAttribute;
            if (String.IsNullOrEmpty(attribute?.AreaName))
                throw new InvalidOperationException($"Missing '{areaRegistrationAttribute}' in assembly '{assembly}'");

            return attribute.AreaName;
        }

    }
}