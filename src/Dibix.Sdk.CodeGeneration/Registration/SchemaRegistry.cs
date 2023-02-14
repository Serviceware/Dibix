using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaRegistry : ISchemaRegistry
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IDictionary<string, SchemaDefinition> _schemas;
        #endregion

        #region Properties
        public IEnumerable<SchemaDefinition> Schemas => _schemas.Values;
        #endregion

        #region Constructor
        public SchemaRegistry(ILogger logger)
        {
            _logger = logger;
            _schemas = new Dictionary<string, SchemaDefinition>();
        }
        #endregion

        #region ISchemaRegistry Members
        public bool IsRegistered(string fullName) => _schemas.ContainsKey(fullName);

        public void Populate(SchemaDefinition schema) => _schemas.Add(schema.FullName, schema);

        public SchemaDefinition GetSchema(SchemaTypeReference schemaTypeReference)
        {
            if (TryGetSchema(schemaTypeReference.Key, out SchemaDefinition schema))
                return schema;

            _logger.LogError($"Could not resolve schema: {schemaTypeReference.Key}", schemaTypeReference.Location.Source, schemaTypeReference.Location.Line, schemaTypeReference.Location.Column);
            return null;
        }

        public TSchema GetSchema<TSchema>(SchemaTypeReference schemaTypeReference) where TSchema : SchemaDefinition
        {
            SchemaDefinition schemaDefinition = GetSchema(schemaTypeReference);
            return CastSchema<TSchema>(schemaDefinition);
        }

        public bool TryGetSchema<TSchema>(string name, out TSchema schema) where TSchema : SchemaDefinition
        {
            if (!_schemas.TryGetValue(name, out SchemaDefinition schemaDefinition))
            {
                schema = default;
                return false;
            }

            schema = CastSchema<TSchema>(schemaDefinition);
            return true;
        }

        public void ImportSchemas(params ISchemaProvider[] schemaProviders) => schemaProviders.SelectMany(schemaProvider => schemaProvider.Collect()).Each(Populate);
        #endregion

        #region Private Methods
        private static TSchema CastSchema<TSchema>(SchemaDefinition schemaDefinition) where TSchema : SchemaDefinition
        {
            if (schemaDefinition is not TSchema expectedSchema)
                throw new InvalidOperationException($"Schema '{schemaDefinition.FullName}' of type '{schemaDefinition.GetType()}' cannot be cast to desired type '{typeof(TSchema)}'");

            return expectedSchema;
        }
        #endregion
    }
}