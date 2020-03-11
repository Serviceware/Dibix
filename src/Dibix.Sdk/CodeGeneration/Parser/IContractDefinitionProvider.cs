using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractDefinitionProvider : ISchemaProvider
    {
        IEnumerable<SchemaDefinition> Contracts { get; }
        bool HasSchemaErrors { get; }
    }
}