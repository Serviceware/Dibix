using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Dibix.Testing
{
    internal sealed class RequiredAttributeConfigurationProxyDecisionStrategy : ConfigurationProxyDecisionStrategy
    {
        protected override bool ShouldBeWrapped(PropertyInfo property)
        {
            bool isRequired = property.IsDefined(typeof(RequiredAttribute));
            return isRequired;
        }
    }
}