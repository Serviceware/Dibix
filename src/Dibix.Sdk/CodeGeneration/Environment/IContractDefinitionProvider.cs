using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractDefinitionProvider
    {
        IEnumerable<ContractDefinition> Contracts { get; }

        bool TryGetContract(string @namespace, string definitionName, out ContractDefinition schema);
    }
}