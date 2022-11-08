using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaDefinitionResolver : ISchemaDefinitionResolver, ISchemaStore
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly ILogger _logger;

        public SchemaDefinitionResolver(ISchemaRegistry schemaRegistry, IExternalSchemaResolver externalSchemaResolver, ILogger logger)
        {
            this._schemaRegistry = schemaRegistry;
            this._externalSchemaResolver = externalSchemaResolver;
            this._logger = logger;
        }

        public SchemaDefinition Resolve(SchemaTypeReference schemaTypeReference)
        {
            if (this._schemaRegistry.TryGetSchema(schemaTypeReference, out SchemaDefinition schemaDefinition))
                return schemaDefinition;

            if (this._externalSchemaResolver.TryGetSchema(schemaTypeReference.Key, out ExternalSchemaDefinition externalSchema))
                return externalSchema.SchemaDefinition;

            this._logger.LogError($"Could not resolve schema: {schemaTypeReference.Key}", schemaTypeReference.Source, schemaTypeReference.Line, schemaTypeReference.Column);
            return null;
        }

        SchemaDefinition ISchemaStore.GetSchema(SchemaTypeReference schemaTypeReference) => this.Resolve(schemaTypeReference);
    }
}