using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISchemaProvider
    {
        IEnumerable<SchemaDefinition> Schemas { get; }
    }
}