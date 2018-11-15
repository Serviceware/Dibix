using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimitiveDataTypeIdentifierSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimitiveDataTypeIdentifierSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 7;
        public override string ErrorMessage => "Primitive data type identifiers must not be quoted: {0}";
    }

    public sealed class PrimitiveDataTypeIdentifierSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SqlDataTypeReference node)
        {
            Identifier identifier = node.Name.Identifiers.Last();
            if (identifier.QuoteType != QuoteType.NotQuoted)
                base.Fail(identifier, identifier.Dump());
        }
    }
}