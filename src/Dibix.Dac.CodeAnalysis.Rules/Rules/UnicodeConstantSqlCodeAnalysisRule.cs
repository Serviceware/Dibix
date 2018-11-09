using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    public sealed class UnicodeConstantSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnicodeConstantSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 4;
        public override string ErrorMessage => "Invalid ascii string literal. Please specify unicode (N'')";
    }

    public sealed class UnicodeConstantSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == TSqlTokenType.AsciiStringLiteral)
                base.Fail(token);
        }
    }
}