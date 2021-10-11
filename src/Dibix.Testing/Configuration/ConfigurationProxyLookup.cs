using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Dibix.Testing
{
    internal sealed class ConfigurationProxyLookup
    {
        public ICollection<PropertyInfo> PrimitiveProperties { get; }
        public IDictionary<PropertyInfo, ConfigurationProxyLookup> ComplexProperties { get; }

        public ConfigurationProxyLookup()
        {
            this.PrimitiveProperties = new Collection<PropertyInfo>();
            this.ComplexProperties = new Dictionary<PropertyInfo, ConfigurationProxyLookup>();
        }
    }
}