using System;
using System.Linq;
using System.Reflection;

namespace Dibix.Testing
{
    internal abstract class ConfigurationProxyDecisionStrategy
    {
        public virtual bool ShouldCreateProxy(ConfigurationProxyLookup lookup) => lookup.PrimitiveProperties.Any() || lookup.ComplexProperties.Any();

        public virtual ConfigurationProxyLookup Collect(Type type)
        {
            ConfigurationProxyLookup lookup = new ConfigurationProxyLookup();
            foreach (PropertyInfo property in type.GetProperties())
            {
                Type propertyType = property.PropertyType;

                if (propertyType.GetConstructor(Type.EmptyTypes) != null)
                {
                    ConfigurationProxyLookup nestedLookup = this.Collect(propertyType);
                    if (!this.ShouldCreateProxy(nestedLookup))
                        continue;

                    lookup.ComplexProperties.Add(property, nestedLookup);
                }
                else
                {
                    bool needsWrapper = this.ShouldBeWrapped(property);
                    if (!needsWrapper)
                        continue;

                    lookup.PrimitiveProperties.Add(property);
                }
            }
            return lookup;
        }

        protected virtual bool ShouldBeWrapped(PropertyInfo property) => false;
    }
}