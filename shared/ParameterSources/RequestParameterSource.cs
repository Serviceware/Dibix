using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    [ActionParameterSource("REQUEST")]
    internal sealed class RequestParameterSource : ActionParameterSourceDefinition<RequestParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public ICollection<PropertyParameterSourceDescriptor> Properties { get; } = new Collection<PropertyParameterSourceDescriptor>
        {
            new PropertyParameterSourceDescriptor("Language", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)),
            new PropertyParameterSourceDescriptor("Languages", new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: true)),
            new PropertyParameterSourceDescriptor("RemoteName", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)),
            new PropertyParameterSourceDescriptor("RemoteAddress", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)),
            new PropertyParameterSourceDescriptor("BearerToken", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)),
            new PropertyParameterSourceDescriptor("BearerTokenExpiresAt", new PrimitiveTypeReference(PrimitiveType.DateTime, isNullable: true, isEnumerable: false))
        };
    }
}