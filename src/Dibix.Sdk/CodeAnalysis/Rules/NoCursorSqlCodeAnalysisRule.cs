using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class NoCursorSqlCodeAnalysisRule : SqlCodeAnalysisRule<NoCursorSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 13;
        public override string ErrorMessage => "Found use of CURSOR statement";
    }

    public sealed class NoCursorSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        protected override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == SqlTokenType.Cursor)
                base.Fail(token);
        }
    }
}