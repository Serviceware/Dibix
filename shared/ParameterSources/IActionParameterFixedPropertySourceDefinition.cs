using System.Collections.Generic;

namespace Dibix
{
    internal interface IActionParameterFixedPropertySourceDefinition
    {
        ICollection<PropertyParameterSourceDescriptor> Properties { get; }
    }
}