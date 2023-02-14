using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    // Resolve schema by probing the registry
    internal sealed class SchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Constructor
        public SchemaTypeResolver(string productName, string areaName, ISchemaRegistry schemaRegistry)
        {
            _productName = productName;
            _areaName = areaName;
            _schemaRegistry = schemaRegistry;
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable)
        {
            NullableTypeName typeName = input;
            if (!TryGetSchemaByProbing(typeName, relativeNamespace, out SchemaDefinition schema)) 
                return null;

            SchemaTypeReference schemaTypeReference = new SchemaTypeReference(schema.FullName, typeName.IsNullable, isEnumerable, location);
            return schemaTypeReference;
        }
        #endregion

        #region Private Methods
        private bool TryGetSchemaByProbing(NullableTypeName typeName, string relativeNamespace, out SchemaDefinition schema)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(_productName, _areaName, LayerName.DomainModel, relativeNamespace, typeName.Name))
            {
                if (_schemaRegistry.TryGetSchema(candidate, out schema))
                    return true;
            }

            schema = null;
            return false;
        }
        #endregion
    }
}