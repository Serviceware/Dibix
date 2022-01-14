using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractDefinitionProvider : ISchemaProvider
    {
        IEnumerable<ContractDefinition> Contracts { get; }
        bool HasSchemaErrors { get; }
    }
}