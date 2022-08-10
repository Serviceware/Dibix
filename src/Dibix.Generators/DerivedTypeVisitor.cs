using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dibix.Generators
{
    internal sealed class DerivedTypeVisitor : SymbolVisitor
    {
        private readonly ITypeSymbol _baseType;

        public ICollection<string> TypeNames { get; }

        public DerivedTypeVisitor(ITypeSymbol baseType)
        {
            this._baseType = baseType;
            this.TypeNames = new HashSet<string>();
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
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
            
            bool baseTypeMatches = SymbolEqualityComparer.Default.Equals(symbol.BaseType, this._baseType);
            if (baseTypeMatches)
                this.TypeNames.Add(symbol.Name);
        }
    }
}