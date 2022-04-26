using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaRegistry : ISchemaRegistry
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IDictionary<string, SchemaDefinition> _schemas;
        #endregion

        #region Properties
        public IEnumerable<SchemaDefinition> Schemas => this._schemas.Values;
        #endregion

        #region Constructor
        public SchemaRegistry(ILogger logger)
        {
            this._logger = logger;
            this._schemas = new Dictionary<string, SchemaDefinition>();
        }
        #endregion

        #region ISchemaRegistry Members
        public bool IsRegistered(string name) => this._schemas.ContainsKey(name);

        public void Populate(SchemaDefinition schema) => this._schemas.Add(schema.FullName, schema);

        public SchemaDefinition GetSchema(SchemaTypeReference reference)
        {
            if (this._schemas.TryGetValue(reference.Key, out SchemaDefinition schema)) 
                return schema;

            this._logger.LogError($"Schema is not registered: {reference.Key}", reference.Source, reference.Line, reference.Column);
            return null;
        }

        public void ImportSchemas(params ISchemaProvider[] schemaProviders) => schemaProviders.SelectMany(schemaProvider => schemaProvider.Schemas).Each(this.Populate);
        #endregion
    }
}