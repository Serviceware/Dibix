using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SchemaRegistry : ISchemaRegistry
    {
        #region Fields
        private readonly IErrorReporter _errorReporter;
        private readonly IDictionary<string, SchemaDefinition> _schemas;
        #endregion

        #region Constructor
        public SchemaRegistry(IErrorReporter errorReporter)
        {
            this._errorReporter = errorReporter;
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

            this._errorReporter.RegisterError(reference.Source, reference.Line, reference.Column, null, $"Schema is not registered: {reference.Key}");
            return null;
        }

        public void ImportSchemas(params ISchemaProvider[] schemaProviders) => schemaProviders.SelectMany(schemaProvider => schemaProvider.Schemas).Each(this.Populate);
        #endregion
    }
}