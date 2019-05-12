using System.Collections.Generic;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IJsonSchemaProvider
    {
        IEnumerable<JsonContract> Schemas { get; }

        bool TryGetSchemaDefinition(string schemaName, string definitionName, out JSchema schema);
    }
}