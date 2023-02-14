using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TypeResolverFacade : ITypeResolverFacade
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IList<TypeResolver> _typeResolvers;
        #endregion

        #region Constructor
        public TypeResolverFacade() => _typeResolvers = new Collection<TypeResolver>();
        public TypeResolverFacade(ILogger logger)
        {
            _logger = logger;
            _typeResolvers = new Collection<TypeResolver> { new PrimitiveTypeResolver() };
        }
        #endregion

        #region ITypeResolverFacade Members
        public void Register(TypeResolver typeResolver) => Register(typeResolver, _typeResolvers.Count);
        public void Register(TypeResolver typeResolver, int position) => _typeResolvers.Insert(position, typeResolver);

        public TypeReference ResolveType(string input, string relativeNamespace, SourceLocation location, bool isEnumerable) => ResolveType(TypeResolutionScope.All, input, relativeNamespace, location, isEnumerable);
        public TypeReference ResolveType(TypeResolutionScope scope, string input, string relativeNamespace, SourceLocation location, bool isEnumerable)
        {
            TypeReference type = null;
            if (!String.IsNullOrEmpty(input))
            {
                type = _typeResolvers.Where(x => x.Scope == scope)
                                     .Select(x => x.ResolveType(input, relativeNamespace, location, isEnumerable))
                                     .FirstOrDefault(x => x != null);
            }

            if (type == null)
                _logger.LogError($"Could not resolve type '{input}'", location.Source, location.Line, location.Column);

            return type;
        }
        #endregion
    }
}