using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchemaTypeResolver : SchemaTypeResolver
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ILogger _logger;
        private readonly IDictionary<string, string> _localUserDefinedTypes;
        private readonly IDictionary<string, Type> _externalUserDefinedTypes;

        public override TypeResolutionScope Scope => TypeResolutionScope.UserDefinedType;

        public UserDefinedTypeSchemaTypeResolver(ISchemaRegistry schemaRegistry, IUserDefinedTypeProvider userDefinedTypeProvider, ReferencedAssemblyInspector referencedAssemblyInspector, ILogger logger) : base(schemaRegistry, userDefinedTypeProvider)
        {
            this._schemaRegistry = schemaRegistry;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._logger = logger;
            this._localUserDefinedTypes = ScanLocalTypes(userDefinedTypeProvider).ToDictionary(x => x.Key, x => x.Value);
            this._externalUserDefinedTypes = new Dictionary<string, Type>();
        }

        public override TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            if (this._localUserDefinedTypes.TryGetValue(input, out string key))
                return new SchemaTypeReference(key, source, line, column, isNullable: false, isEnumerable: false);

            if (this.TryGetExternalType(input, out Type type))
                return ReflectionTypeResolver.ResolveType(type, source, line, column, input, this._schemaRegistry, this._logger);

            return base.ResolveType(input, @namespace, source, line, column, isEnumerable);
        }

        private bool TryGetExternalType(string input, out Type externalType)
        {
            if (this._externalUserDefinedTypes.TryGetValue(input, out externalType))
                return true;

            Type matchingType = this._referencedAssemblyInspector.Inspect(x => x.Where(y => y.IsArtifactAssembly())
                                                                                .SelectMany(y => y.GetTypes())
                                                                                .FirstOrDefault(y => y.GetUdtName() == input));

            if (matchingType != null)
            {
                this._externalUserDefinedTypes.Add(input, matchingType);
                externalType = matchingType;
                return true;
            }

            return false;
        }

        private static IEnumerable<KeyValuePair<string, string>> ScanLocalTypes(IUserDefinedTypeProvider userDefinedTypeProvider)
        {
            return userDefinedTypeProvider.Types.Select(x => new KeyValuePair<string, string>(x.UdtName, x.FullName));
        }
    }
}