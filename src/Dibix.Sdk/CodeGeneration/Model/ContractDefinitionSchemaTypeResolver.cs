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
        private readonly string _rootNamespace;
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
          , string rootNamespace
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
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._externalSchemas = new Dictionary<string, Type>();
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;
            if (this.TryGetLocalSchema(input, relativeNamespace, out SchemaDefinition schema)
             || this.TryGetExternalSchema(input, relativeNamespace, out schema))
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
        private bool TryGetLocalSchema(NullableTypeName typeName, string relativeNamespace, out SchemaDefinition schema)
        {
            if (!String.IsNullOrEmpty(relativeNamespace))
            {
                string absoluteNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, relativeNamespace);
                string key = $"{absoluteNamespace}.{typeName.Name}";
                if (!this._contractDefinitionProvider.TryGetSchema(key, out schema))
                {
                    // Scenario:
                    // We are inside a namespace group but the type reference includes this namespace group aswell, since it's assumed that all type references are relative to root.
                    // This is actually a different behavior, as in C#, for example.
                    // Because there, when you are inside a namespace group and you want to get out of it, the type reference must be absolute.
                    // Unfortunately fixing this would introduce a breaking change. Therefore we allow it.
                    string absoluteTypeName = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, typeName.Name);
                    if (!this._contractDefinitionProvider.TryGetSchema(absoluteTypeName, out schema))
                    {
                        schema = null;
                        return false;
                    }
                }
            }
            else
            {
                // Assume absolute type name
                if (!this._contractDefinitionProvider.TryGetSchema(typeName.Name, out schema))
                {
                    // Assume relative type name
                    string absoluteTypeName = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, typeName.Name);
                    if (!this._contractDefinitionProvider.TryGetSchema(absoluteTypeName, out schema))
                    {
                        schema = null;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGetExternalSchema(string input, string relativeNamespace, out SchemaDefinition schema)
        {
            // This sounds more like something for ReflectionTypeResolver => Skip for performance reasons
            if (input.Contains(","))
            {
                schema = null;
                return false;
            }

            string absoluteTypeName;
            if (!String.IsNullOrEmpty(relativeNamespace))
            {
                string absoluteNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, relativeNamespace);
                absoluteTypeName = $"{absoluteNamespace}.{input}";
            }
            else
            {
                absoluteTypeName = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, input);
            }

            // Assume absolute type name
            if (this.TryGetExternalSchema(input, out schema))
                return true;

            // Assume relative type name
            if (this.TryGetExternalSchema(absoluteTypeName, out schema))
                return true;

            return false;
        }
        private bool TryGetExternalSchema(string fullName, out SchemaDefinition schema)
        {
            if (this._externalSchemaResolver.TryGetSchema(fullName, out ExternalSchemaDefinition externalSchemaDefinition))
            {
                schema = externalSchemaDefinition.SchemaDefinition;
                return true;
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

            string absoluteTypeName;
            if (!String.IsNullOrEmpty(relativeNamespace))
            {
                string absoluteNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, relativeNamespace);
                absoluteTypeName = $"{absoluteNamespace}.{input}";
            }
            else
            {
                absoluteTypeName = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, input);
            }

            Type matchingType = this._referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => IsMatchingType(input, absoluteTypeName, y)));

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