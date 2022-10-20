using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 13)]
    public sealed class NoCursorSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Found use of CURSOR statement";

        public NoCursorSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        protected override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == SqlTokenType.Cursor)
                base.Fail(token);
        }
    }
}