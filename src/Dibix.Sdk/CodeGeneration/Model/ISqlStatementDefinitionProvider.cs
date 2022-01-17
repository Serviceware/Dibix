using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementDefinitionProvider : ISchemaProvider
    {
        IEnumerable<SqlStatementDefinition> SqlStatements { get; }

        bool TryGetDefinition(string @namespace, string definitionName, out SqlStatementDefinition definition);
    }
}