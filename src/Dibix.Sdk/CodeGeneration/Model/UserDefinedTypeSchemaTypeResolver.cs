using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchemaTypeResolver : TypeResolver
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ILogger _logger;
        private readonly IDictionary<string, UserDefinedTypeSchema> _localSchemas;
        private readonly IDictionary<string, UserDefinedTypeSchema> _externalSchemas;
        private readonly IDictionary<string, Type> _externalTypes;

        public override TypeResolutionScope Scope => TypeResolutionScope.UserDefinedType;

        public UserDefinedTypeSchemaTypeResolver
        (
            ISchemaRegistry schemaRegistry
          , IUserDefinedTypeProvider userDefinedTypeProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ILogger logger
        )
        {
            this._schemaRegistry = schemaRegistry;
            this._externalSchemaResolver = externalSchemaResolver;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._logger = logger;
            this._localSchemas = userDefinedTypeProvider.Types.ToDictionary(x => x.UdtName);
            this._externalSchemas = externalSchemaResolver.Schemas
                                                          .Select(x => x.SchemaDefinition)
                                                          .OfType<UserDefinedTypeSchema>()
                                                          .ToDictionary(x => x.UdtName);
            this._externalTypes = new Dictionary<string, Type>();
        }

        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            if (this.TryGetLocalSchema(input, relativeNamespace, out UserDefinedTypeSchema schema)
             || this.TryGetExternalSchema(input, relativeNamespace, out schema)
             || this.TryGetExternalSchemaByUDTName(input, relativeNamespace, out schema))
            {
                SchemaTypeReference schemaTypeReference = new SchemaTypeReference(schema.FullName, isNullable: false, isEnumerable: false, source, line, column);
                return schemaTypeReference;
            }

            //if (this.TryGetExternalType(input, out Type type))
            //    return ReflectionTypeResolver.ResolveType(type, source, line, column, input, this._schemaRegistry, this._logger);

            return null;
        }

        private bool TryGetLocalSchema(NullableTypeName typeName, string relativeNamespace, out UserDefinedTypeSchema schema)
        {
            return this._localSchemas.TryGetValue(typeName.Name, out schema);
        }

        private bool TryGetExternalSchema(string input, string relativeNamespace, out UserDefinedTypeSchema schemaDefinition)
        {
            if (this._externalSchemaResolver.TryGetSchema(input, out ExternalSchemaDefinition externalSchemaDefinition))
            {
                schemaDefinition = externalSchemaDefinition.GetSchema<UserDefinedTypeSchema>();
                return true;
            }

            schemaDefinition = null;
            return false;
        }

        private bool TryGetExternalSchemaByUDTName(string input, string relativeNamespace, out UserDefinedTypeSchema schemaDefinition)
        {
            if (this._externalSchemas.TryGetValue(input, out schemaDefinition))
            {
                if (!this._schemaRegistry.IsRegistered(schemaDefinition.FullName))
                    this._schemaRegistry.Populate(schemaDefinition);

                return true;
            }

            return false;
        }

        private bool TryGetExternalType(string input, out Type externalType)
        {
            if (this._externalTypes.TryGetValue(input, out externalType))
                return true;

            Type matchingType = this._referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => IsMatchingType(input, y)));

            if (matchingType != null)
            {
                this._externalTypes.Add(input, matchingType);
                externalType = matchingType;
                return true;
            }

            return false;
        }

        private static bool IsMatchingType(string input, Type type) => type.GetUdtName() == input;
    }
}