using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchemaTypeResolver : TypeResolver
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ILogger _logger;
        private readonly IDictionary<string, string> _localSchemas;
        private readonly IDictionary<string, Type> _externalSchemas;

        public override TypeResolutionScope Scope => TypeResolutionScope.UserDefinedType;

        public UserDefinedTypeSchemaTypeResolver(ISchemaRegistry schemaRegistry, IUserDefinedTypeProvider userDefinedTypeProvider, ReferencedAssemblyInspector referencedAssemblyInspector, ILogger logger)
        {
            this._schemaRegistry = schemaRegistry;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._logger = logger;
            this._localSchemas = ScanLocalTypes(userDefinedTypeProvider).ToDictionary(x => x.Key, x => x.Value);
            this._externalSchemas = new Dictionary<string, Type>();
        }

        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            if (this._localSchemas.TryGetValue(input, out string key))
                return new SchemaTypeReference(key, isNullable: false, isEnumerable: false, source: source, line: line, column: column);

            if (this.TryGetExternalType(input, out Type type))
                return ReflectionTypeResolver.ResolveType(type, source, line, column, input, this._schemaRegistry, this._logger);

            return null;
        }

        private bool TryGetExternalType(string input, out Type externalType)
        {
            if (this._externalSchemas.TryGetValue(input, out externalType))
                return true;

            Type matchingType = this._referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => IsMatchingType(input, y)));

            if (matchingType != null)
            {
                this._externalSchemas.Add(input, matchingType);
                externalType = matchingType;
                return true;
            }

            return false;
        }

        private static bool IsMatchingType(string input, Type type) => type.GetUdtName() == input;

        private static IEnumerable<KeyValuePair<string, string>> ScanLocalTypes(IUserDefinedTypeProvider userDefinedTypeProvider)
        {
            return userDefinedTypeProvider.Types.Select(x => new KeyValuePair<string, string>(x.UdtName, x.FullName));
        }
    }
}