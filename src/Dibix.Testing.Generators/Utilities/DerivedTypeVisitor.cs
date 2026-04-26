using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Dibix.Testing.Generators
{
    internal sealed class DerivedTypeVisitor : SymbolVisitor
    {
        private readonly ITypeSymbol _baseType;
        private readonly CancellationToken _cancellationToken;
        private readonly ImmutableArray<string>.Builder _typeNames = ImmutableArray.CreateBuilder<string>();

        public ImmutableArray<string> TypeNames => _typeNames.ToImmutable();

        public DerivedTypeVisitor(ITypeSymbol baseType, CancellationToken cancellationToken)
        {
            _baseType = baseType;
            _cancellationToken = cancellationToken;
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            foreach (INamespaceOrTypeSymbol member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            foreach (ISymbol member in symbol.GetMembers())
            {
                member.Accept(this);
            }

            bool baseTypeMatches = SymbolEqualityComparer.Default.Equals(symbol.BaseType, _baseType);
            if (baseTypeMatches)
                _typeNames.Add(symbol.Name);
        }
    }
}