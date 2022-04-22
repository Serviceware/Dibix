using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.Model
{
    public interface IExternalSchemaResolver
    {
        ICollection<ExternalSchemaDefinition> Schemas { get; }

        bool TryGetSchema(string fullName, out ExternalSchemaDefinition schema);
    }
}