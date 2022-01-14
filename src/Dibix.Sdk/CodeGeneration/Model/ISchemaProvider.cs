using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaProvider
    {
        IEnumerable<SchemaDefinition> Schemas { get; }

        bool TryGetSchema(string name, out SchemaDefinition schema);
    }
}