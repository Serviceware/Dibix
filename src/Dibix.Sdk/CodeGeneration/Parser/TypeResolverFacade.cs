using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TypeResolverFacade : ITypeResolverFacade
    {
        #region Fields
        private readonly IErrorReporter _errorReporter;
        private readonly IList<ITypeResolver> _typeResolvers;
        #endregion

        #region Constructor
        public TypeResolverFacade() => this._typeResolvers = new Collection<ITypeResolver>();
        public TypeResolverFacade(AssemblyResolver assemblyResolver, ISchemaRegistry schemaRegistry, IErrorReporter errorReporter)
        {
            this._errorReporter = errorReporter;
            this._typeResolvers = new Collection<ITypeResolver> { new ReflectionTypeResolver(assemblyResolver, schemaRegistry, errorReporter) };
        }
        #endregion

        #region ITypeResolverFacade Members
        public void Register(ITypeResolver typeResolver) => this.Register(typeResolver, this._typeResolvers.Count);
        public void Register(ITypeResolver typeResolver, int position) => this._typeResolvers.Insert(position, typeResolver);

        public TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            TypeReference type = this._typeResolvers.Select(x => x.ResolveType(input, @namespace, source, line, column, isEnumerable)).FirstOrDefault(x => x != null);
            if (type == null)
                this._errorReporter.RegisterError(source, line, column, null, $"Could not resolve type '{input}'");

            return type;
        }
        #endregion
    }
}