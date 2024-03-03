using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    [ActionParameterSource("ENV")]
    internal sealed class EnvironmentParameterSource : ActionParameterSourceDefinition<EnvironmentParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public ICollection<string> Properties { get; } = new Collection<string>
        {
            "CurrentProcessId",
            "MachineName"
        };
    }
}