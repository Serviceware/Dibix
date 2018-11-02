using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public sealed class SqlUnicodeLintRule : SqlLintRule
    {
        public override int Id { get { return 4; } }
        public override string ErrorMessage { get { return "Invalid ascii string literal. Please specify unicode (N'')"; } }

        protected override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == TSqlTokenType.AsciiStringLiteral)
                base.Fail(token);
        }
    }
}