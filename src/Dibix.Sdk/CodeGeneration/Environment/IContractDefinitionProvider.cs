using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractDefinitionProvider
    {
        ICollection<ContractDefinition> Contracts { get; }
        bool HasSchemaErrors { get; }

        bool TryGetContract(string contractName, out ContractDefinition schema);
    }
}