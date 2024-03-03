using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    [ActionParameterSource("REQUEST")]
    internal sealed class RequestParameterSource : ActionParameterSourceDefinition<RequestParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public ICollection<string> Properties { get; } = new Collection<string>
        {
            "Language",
            "Languages",
            "RemoteName",
            "RemoteAddress",
            "BearerToken",
            "BearerTokenExpiresAt"
        };
    }
}