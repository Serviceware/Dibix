using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 7)]
    public sealed class PrimitiveDataTypeIdentifierSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Primitive data type identifiers must not be quoted: {0}";

        public override void Visit(SqlDataTypeReference node)
        {
            Identifier identifier = node.Name.BaseIdentifier;
            if (identifier.QuoteType != QuoteType.NotQuoted)
                base.Fail(identifier, identifier.Dump());
        }
    }
}