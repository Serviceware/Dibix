using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IActionParameterConverterRegistry
    {
        bool IsRegistered(string name);
        void Register(string name, IEnumerable<string> requiredClaims);
        ActionParameterConverterRegistration GetRegistration(string converterName);
    }
}