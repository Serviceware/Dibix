using System;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly ISchemaProvider _schemaProvider;
        #endregion

        #region Constructor
        protected SchemaTypeResolver(ISchemaProvider schemaProvider)
        {
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

            // TODO: Investigate use case
            //if (!this._schemaRegistry.IsRegistered(type.Key))
            //{
            //    //this._schemaRegistry.Populate(schema);
            //}

            return type;
        }
        #endregion
    }
}