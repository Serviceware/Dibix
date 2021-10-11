using System.Reflection;

namespace Dibix.Testing
{
    internal sealed class VirtualMemberConfigurationProxyDecisionStrategy : ConfigurationProxyDecisionStrategy
    {
        protected override bool ShouldBeWrapped(PropertyInfo property)
        {
            bool isVirtual = (property.GetMethod?.IsVirtual).GetValueOrDefault(false) && (property.SetMethod?.IsVirtual).GetValueOrDefault(false);
            return isVirtual;
        }
    }
}