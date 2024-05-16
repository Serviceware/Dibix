using System.Collections.Generic;

namespace Dibix
{
    internal sealed class DynamicPropertyParameterSource : ActionParameterSourceDefinition, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name { get; }
        public ICollection<PropertyParameterSourceDescriptor> Properties { get; }

        public DynamicPropertyParameterSource(string name, ICollection<PropertyParameterSourceDescriptor> properties)
        {
            Name = name;
            Properties = properties;
        }
    }
}