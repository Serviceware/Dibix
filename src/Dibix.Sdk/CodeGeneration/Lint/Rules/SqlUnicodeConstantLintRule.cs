using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Lint.Rules
{
    public sealed class SqlUnicodeConstantLintRule : SqlLintRule
    {
        public override int Id => 4;
        public override string ErrorMessage => "Invalid ascii string literal. Please specify unicode (N'')";

        protected override void Visit(TSqlParserToken token)
        {
            if (token.TokenType == TSqlTokenType.AsciiStringLiteral)
                base.Fail(token);
        }
    }
}