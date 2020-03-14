using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SchemaTypeResolver : ITypeResolver
    {
        #region Fields
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ICollection<ISchemaProvider> _schemaProviders;
        #endregion

        #region Constructor
        public SchemaTypeResolver(ISchemaRegistry schemaRegistry, IContractDefinitionProvider contractDefinitionProvider, IUserDefinedTypeProvider userDefinedTypeProvider)
        {
            this._schemaRegistry = schemaRegistry;
            this._schemaProviders = new ISchemaProvider[] { contractDefinitionProvider, userDefinedTypeProvider };
        }
        #endregion

        #region ITypeResolver Members
        public TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            foreach (ISchemaProvider schemaProvider in this._schemaProviders)
            {
                if (!schemaProvider.TryGetSchema(typeName.Name, out SchemaDefinition schema))
                {
                    if (String.IsNullOrEmpty(@namespace))
                        continue;

                    // Try relative to local namespace
                    if (!schemaProvider.TryGetSchema($"{@namespace}.{typeName.Name}", out schema))
                        continue;
                }

                SchemaTypeReference type = new SchemaTypeReference(schema.FullName, source, line, column, typeName.IsNullable, isEnumerable);
                if (!this._schemaRegistry.IsRegistered(type.Key))
                    this._schemaRegistry.Populate(schema);

                return type;
            }

            return null;
        }
        #endregion
    }
}