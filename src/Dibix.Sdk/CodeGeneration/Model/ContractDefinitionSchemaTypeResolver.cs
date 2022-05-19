using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly AssemblyResolver _assemblyResolver;
        private readonly ILogger _logger;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IDictionary<string, Type> _externalSchemas;
        #endregion

        #region Constructor
        public ContractDefinitionSchemaTypeResolver
        (
            ISchemaRegistry schemaRegistry
          , IContractDefinitionProvider contractDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , AssemblyResolver assemblyResolver
          , ILogger logger
          , string productName
          , string areaName
        )
        {
            this._schemaRegistry = schemaRegistry;
            this._contractDefinitionProvider = contractDefinitionProvider;
            this._externalSchemaResolver = externalSchemaResolver;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._assemblyResolver = assemblyResolver;
            this._logger = logger;
            this._productName = productName;
            this._areaName = areaName;
            this._externalSchemas = new Dictionary<string, Type>();
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;
            if (this.TryGetSchemaByProbing(typeName, relativeNamespace, out SchemaDefinition schema))
            {
                SchemaTypeReference schemaTypeReference = new SchemaTypeReference(schema.FullName, typeName.IsNullable, isEnumerable, source, line, column);
                return schemaTypeReference;
            }

            //if (this.TryGetExternalType(input, relativeNamespace, out Type type))
            //    return ReflectionTypeResolver.ResolveForeignType(type, source, line, column, typeName.IsNullable, isEnumerable, this._schemaRegistry, this._logger, this._assemblyResolver);

            return null;
        }
        #endregion

        #region Private Methods
        private bool TryGetSchemaByProbing(NullableTypeName typeName, string relativeNamespace, out SchemaDefinition schema)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(this._productName, this._areaName, LayerName.DomainModel, relativeNamespace, typeName.Name))
            {
                // Try local schema
                if (this._contractDefinitionProvider.TryGetSchema(candidate, out schema))
                {
                    return true;
                }

                // Try external schema
                if (this._externalSchemaResolver.TryGetSchema(candidate, out ExternalSchemaDefinition externalSchemaDefinition))
                {
                    schema = externalSchemaDefinition.SchemaDefinition;
                    return true;
                }
            }

            schema = null;
            return false;
        }

        private bool TryGetExternalType(string input, string relativeNamespace, out Type externalType)
        {
            if (this._externalSchemas.TryGetValue(input, out externalType))
                return true;

            // This sounds more like something for ReflectionTypeResolver => Skip for performance reasons
            if (input.Contains(","))
                return false;
            
            TargetPath absoluteTypeName = PathUtility.BuildAbsoluteTargetName(this._productName, this._areaName, LayerName.DomainModel, relativeNamespace, input);
            Type matchingType = this._referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => IsMatchingType(input, absoluteTypeName.Path, y)));

            if (matchingType != null)
            {
                this._externalSchemas.Add(input, matchingType);
                externalType = matchingType;
                return true;
            }

            return false;
        }

        private static bool IsMatchingType(string input, string absoluteTypeName, Type type)
        {
            // Assume absolute type name
            if (type.FullName == input)
                return true;

            // Assume relative type name
            if (type.FullName == absoluteTypeName)
                return true;

            return false;
        }
        #endregion
    }
}