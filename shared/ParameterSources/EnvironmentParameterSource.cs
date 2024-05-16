using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    [ActionParameterSource("ENV")]
    internal sealed class EnvironmentParameterSource : ActionParameterSourceDefinition<EnvironmentParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public ICollection<PropertyParameterSourceDescriptor> Properties { get; } = new Collection<PropertyParameterSourceDescriptor>
        {
            new PropertyParameterSourceDescriptor("CurrentProcessId", new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false)),
            new PropertyParameterSourceDescriptor("MachineName", new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false))
        };
    }
}