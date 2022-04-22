using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TypeResolverFacade : ITypeResolverFacade
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IList<TypeResolver> _typeResolvers;
        #endregion

        #region Constructor
        public TypeResolverFacade() => this._typeResolvers = new Collection<TypeResolver>();
        public TypeResolverFacade(AssemblyResolver assemblyResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._logger = logger;
            this._typeResolvers = new Collection<TypeResolver>
            {
                new PrimitiveTypeResolver()
            //, new ReflectionTypeResolver(assemblyResolver, schemaRegistry, logger)
            };
        }
        #endregion

        #region ITypeResolverFacade Members
        public void Register(TypeResolver typeResolver) => this.Register(typeResolver, this._typeResolvers.Count);
        public void Register(TypeResolver typeResolver, int position) => this._typeResolvers.Insert(position, typeResolver);

        public TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable) => this.ResolveType(TypeResolutionScope.All, input, relativeNamespace, source, line, column, isEnumerable);
        public TypeReference ResolveType(TypeResolutionScope scope, string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            TypeReference type = this._typeResolvers
                                     .Where(x => x.Scope == scope)
                                     .Select(x => x.ResolveType(input, relativeNamespace, source, line, column, isEnumerable))
                                     .FirstOrDefault(x => x != null);

            if (type == null)
                this._logger.LogError(null, $"Could not resolve type '{input}'", source, line, column);

            return type;
        }
        #endregion
    }
}