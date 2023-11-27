using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    internal sealed class RequestParameterSource : ActionParameterSourceDefinition<RequestParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name => "REQUEST";
        public ICollection<string> Properties { get; } = new Collection<string>
        {
            "Language",
            "Languages",
            "RemoteName",
            "RemoteAddress",
            "BearerToken",
            "BearerTokenExpiresIn"
        };
    }
}