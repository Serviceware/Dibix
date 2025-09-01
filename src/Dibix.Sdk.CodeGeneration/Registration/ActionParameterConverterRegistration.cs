using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterConverterRegistration
    {
        public string Name { get; }
        public IReadOnlyCollection<string> RequiredClaims { get; }

        public ActionParameterConverterRegistration(string name, IEnumerable<string> requiredClaims)
        {
            Name = name;
            RequiredClaims = requiredClaims.ToArray();
        }
    }
}