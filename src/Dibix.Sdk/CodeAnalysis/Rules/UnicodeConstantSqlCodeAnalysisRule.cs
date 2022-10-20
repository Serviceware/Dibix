using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 4)]
    public sealed class UnicodeConstantSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Invalid ascii string literal. Please specify unicode (N'')";

        public UnicodeConstantSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        protected override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == SqlTokenType.AsciiStringLiteral)
                base.Fail(token);
        }
    }
}