using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaTypeResolver : TypeResolver
    {
        #region Fields
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ILogger _logger;
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IDictionary<string, Type> _externalSchemas;
        #endregion

        #region Constructor
        public ContractDefinitionSchemaTypeResolver(ISchemaRegistry schemaRegistry, IContractDefinitionProvider contractDefinitionProvider, ReferencedAssemblyInspector referencedAssemblyInspector, ILogger logger, string rootNamespace, string productName, string areaName)
        {
            this._schemaRegistry = schemaRegistry;
            this._contractDefinitionProvider = contractDefinitionProvider;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
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
            if (this.TryGetLocalType(input, relativeNamespace, source, line, column, isEnumerable, out SchemaTypeReference schemaTypeReference))
                return schemaTypeReference;

            if (this.TryGetExternalType(input, relativeNamespace, out Type type))
                return ReflectionTypeResolver.ResolveType(type, source, line, column, typeName.IsNullable, isEnumerable, this._schemaRegistry, this._logger);

            return null;
        }
        #endregion

        #region Private Methods
        private bool TryGetLocalType(NullableTypeName typeName, string relativeNamespace, string source, int line, int column, bool isEnumerable, out SchemaTypeReference schemaTypeReference)
        {
            SchemaDefinition schema;

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
                        schemaTypeReference = null;
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
                        schemaTypeReference = null;
                        return false;
                    }
                }
            }

            schemaTypeReference = new SchemaTypeReference(schema.FullName, typeName.IsNullable, isEnumerable, source, line, column);
            return true;
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