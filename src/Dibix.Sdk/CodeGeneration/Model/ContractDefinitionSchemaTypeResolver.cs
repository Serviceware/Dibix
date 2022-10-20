using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly ArtifactGenerationConfiguration _configuration;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly AssemblyResolver _assemblyResolver;
        private readonly ILogger _logger;
        private readonly IDictionary<string, Type> _externalSchemas;
        #endregion

        #region Constructor
        public ContractDefinitionSchemaTypeResolver
        (
            ArtifactGenerationConfiguration configuration
          , ISchemaRegistry schemaRegistry
          , IContractDefinitionProvider contractDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , AssemblyResolver assemblyResolver
          , ILogger logger
        )
        {
            _configuration = configuration;
            _schemaRegistry = schemaRegistry;
            _contractDefinitionProvider = contractDefinitionProvider;
            _externalSchemaResolver = externalSchemaResolver;
            _referencedAssemblyInspector = referencedAssemblyInspector;
            _assemblyResolver = assemblyResolver;
            _logger = logger;
            _externalSchemas = new Dictionary<string, Type>();
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;
            if (TryGetSchemaByProbing(typeName, relativeNamespace, out SchemaDefinition schema))
            {
                SchemaTypeReference schemaTypeReference = new SchemaTypeReference(schema.FullName, typeName.IsNullable, isEnumerable, source, line, column);
                return schemaTypeReference;
            }

            //if (TryGetExternalType(input, relativeNamespace, out Type type))
            //    return ReflectionTypeResolver.ResolveForeignType(type, source, line, column, typeName.IsNullable, isEnumerable, _schemaRegistry, _logger, _assemblyResolver);

            return null;
        }
        #endregion

        #region Private Methods
        private bool TryGetSchemaByProbing(NullableTypeName typeName, string relativeNamespace, out SchemaDefinition schema)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(_configuration.ProductName, _configuration.AreaName, LayerName.DomainModel, relativeNamespace, typeName.Name))
            {
                // Try local schema
                if (_contractDefinitionProvider.TryGetSchema(candidate, out schema))
                {
                    return true;
                }

                // Try external schema
                if (_externalSchemaResolver.TryGetSchema(candidate, out ExternalSchemaDefinition externalSchemaDefinition))
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
            if (_externalSchemas.TryGetValue(input, out externalType))
                return true;

            // This sounds more like something for ReflectionTypeResolver => Skip for performance reasons
            if (input.Contains(","))
                return false;
            
            TargetPath absoluteTypeName = PathUtility.BuildAbsoluteTargetName(_configuration.ProductName, _configuration.AreaName, LayerName.DomainModel, relativeNamespace, input);
            Type matchingType = _referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => IsMatchingType(input, absoluteTypeName.Path, y)));

            if (matchingType != null)
            {
                _externalSchemas.Add(input, matchingType);
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