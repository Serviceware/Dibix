using System;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ISchemaProvider _schemaProvider;
        #endregion

        #region Constructor
        protected SchemaTypeResolver(ISchemaRegistry schemaRegistry, ISchemaProvider schemaProvider)
        {
            this._schemaRegistry = schemaRegistry;
            this._schemaProvider = schemaProvider;
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            if (!this._schemaProvider.TryGetSchema(typeName.Name, out SchemaDefinition schema))
            {
                if (String.IsNullOrEmpty(@namespace))
                    return null;

                // Try relative to local namespace
                if (!this._schemaProvider.TryGetSchema($"{@namespace}.{typeName.Name}", out schema))
                    return null;
            }

            SchemaTypeReference type = new SchemaTypeReference(schema.FullName, typeName.IsNullable, isEnumerable, source, line, column);
            if (!this._schemaRegistry.IsRegistered(type.Key))
                this._schemaRegistry.Populate(schema);

            return type;
        }
        #endregion
    }
}