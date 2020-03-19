using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchemaTypeResolver : SchemaTypeResolver
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IDictionary<string, string> _localUserDefinedTypes;
        private readonly IDictionary<string, Type> _externalUserDefinedTypes;

        public override TypeResolutionScope Scope => TypeResolutionScope.UserDefinedType;

        public UserDefinedTypeSchemaTypeResolver(ISchemaRegistry schemaRegistry, IUserDefinedTypeProvider userDefinedTypeProvider, IReferencedAssemblyProvider referencedAssemblyProvider) : base(schemaRegistry, userDefinedTypeProvider)
        {
            this._schemaRegistry = schemaRegistry;
            this._localUserDefinedTypes = ScanLocalTypes(userDefinedTypeProvider).ToDictionary(x => x.Key, x => x.Value);
            this._externalUserDefinedTypes = ScanExternalTypes(referencedAssemblyProvider);
        }

        public override TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            if (this._localUserDefinedTypes.TryGetValue(input, out string key))
                return new SchemaTypeReference(key, source, line, column, false, false);

            if (this._externalUserDefinedTypes.TryGetValue(input, out Type type))
                return ReflectionTypeResolver.ResolveType(type, source, line, column, false, false, this._schemaRegistry);

            return base.ResolveType(input, @namespace, source, line, column, isEnumerable);
        }

        private static IEnumerable<KeyValuePair<string, string>> ScanLocalTypes(IUserDefinedTypeProvider userDefinedTypeProvider)
        {
            return userDefinedTypeProvider.Types.Select(x => new KeyValuePair<string, string>(x.UdtName, x.FullName));
        }

        private static IDictionary<string, Type> ScanExternalTypes(IReferencedAssemblyProvider referencedAssemblyProvider)
        {
            try
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
                return ScanExternalTypesCore(referencedAssemblyProvider).ToDictionary(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
            }
        }

        private static IEnumerable<KeyValuePair<string, Type>> ScanExternalTypesCore(IReferencedAssemblyProvider referencedAssemblyProvider)
        {
            return from assembly in referencedAssemblyProvider.ReferencedAssemblies
                   where assembly.IsDefined("Dibix.ArtifactAssemblyAttribute")
                   from type in assembly.GetTypes()
                   let udtName = CustomAttributeData.GetCustomAttributes(type)
                                                    .Where(x => x.AttributeType.FullName == "Dibix.StructuredTypeAttribute")
                                                    .Select(x => (string)x.ConstructorArguments.Select(y => y.Value).Single())
                                                    .FirstOrDefault()
                   where udtName != null
                   select new KeyValuePair<string, Type>(udtName, type);
        }

        // Cannot resolve dependency to assembly '' because it has not been preloaded.
        // When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.
        private static Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (assembly != null)
                return assembly;

            assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (assembly != null)
                return Assembly.ReflectionOnlyLoadFrom(assembly.Location);

            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}