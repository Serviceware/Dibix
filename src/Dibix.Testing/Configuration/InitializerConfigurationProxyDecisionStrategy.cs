using System;
using System.Reflection;

namespace Dibix.Testing
{
    internal sealed class InitializerConfigurationProxyDecisionStrategy : ConfigurationProxyDecisionStrategy
    {
        protected override bool ShouldBeWrapped(PropertyInfo property)
        {
            FieldInfo backingField = property.DeclaringType.GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (backingField == null)
                return false;

            ConstructorInfo constructor = property.DeclaringType.GetConstructor(new Type[0]);
            if (constructor == null)
                return false;

            // TODO: Analyze the IL to determine if the property has an initializer (by setting the backing field to a value in the ctor)
            return false;
        }
    }
}