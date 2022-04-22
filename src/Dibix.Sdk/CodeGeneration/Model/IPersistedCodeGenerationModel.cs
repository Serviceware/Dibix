using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IPersistedCodeGenerationModel
    {
        string DefaultClassName { get; }
        ICollection<SqlStatementDefinition> SqlStatements { get; }
        ICollection<SchemaDefinition> Schemas { get; }
    }
}