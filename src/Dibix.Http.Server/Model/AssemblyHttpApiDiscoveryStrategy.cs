using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class AssemblyHttpApiDiscoveryStrategy : IHttpApiDiscoveryStrategy
    {
        #region Fields
        private readonly IEnumerable<Assembly> _assemblies;
        #endregion

        #region Constructor
        public AssemblyHttpApiDiscoveryStrategy(IEnumerable<Assembly> assemblies)
        {
            this._assemblies = assemblies;
        }
        #endregion

        #region IHttpApiDiscoveryStrategy Members
        public IEnumerable<HttpApiDescriptor> Collect(IHttpApiDiscoveryContext context)
        {
            foreach (Assembly assembly in this._assemblies)
            {
                AreaRegistrationAttribute attribute = assembly.GetCustomAttribute<AreaRegistrationAttribute>();
                if (attribute == null)
                    continue;

                if (String.IsNullOrEmpty(attribute.AreaName))
                    throw new InvalidOperationException($"Area name in api registration cannot be empty: {assembly.GetName().Name}");

                ICollection<Type> types = assembly.GetLoadableTypes().ToArray();

                Type apiDescriptorType = types.FirstOrDefault(typeof(HttpApiDescriptor).IsAssignableFrom);
                HttpApiDescriptor descriptor = apiDescriptorType != null ? (HttpApiDescriptor)Activator.CreateInstance(apiDescriptorType) : new HttpApiRegistration(assembly);
                descriptor.Configure(context);
                yield return descriptor;
            }
        }
        #endregion
    }
}