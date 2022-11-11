using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 8)]
    public sealed class ImplicitSyntaxSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0}";

        public ImplicitSyntaxSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(TableReferenceWithAlias node)
        {
            if (node.Alias == null)
                return;

            for (int i = node.Alias.FirstTokenIndex - 1; i > node.FirstTokenIndex; i--)
            {
                TSqlParserToken token = node.ScriptTokenStream[i];
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                    continue;

                if (token.TokenType == SqlTokenType.As)
                    return;
                
                break;
            }

            ReportImplicitAliasError(node.Alias);
        }

        public override void Visit(SelectScalarExpression node)
        {
            if (node.ColumnName == null)
                return;

            // SELECT [idx] = [x].[id]
            if (node.ColumnName.FirstTokenIndex < node.Expression.FirstTokenIndex)
                return;

            for (int i = node.ColumnName.FirstTokenIndex; i > node.FirstTokenIndex; i--)
            {
                if (node.ScriptTokenStream[i].TokenType == SqlTokenType.As)
                    return;
            }
            ReportImplicitAliasError(node.ColumnName);
        }

        public override void Visit(InsertSpecification node)
        {
            if (node.InsertOption == InsertOption.None)
                Fail(node.Target, "Use INSERT INTO rather than just INSERT");
        }

        public override void Visit(DeleteSpecification node)
        {
            if (node.FromClause != null)
                return;

            for (int i = node.Target.FirstTokenIndex - 1; i > node.FirstTokenIndex; i--)
            {
                TSqlParserToken token = node.ScriptTokenStream[i];
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                    continue;

                if (token.TokenType == SqlTokenType.From)
                    return;

                break;
            }
            Fail(node.Target, "Use DELETE FROM rather than just DELETE");
        }

        private void ReportImplicitAliasError(TSqlFragment node) => Fail(node, "Aliases must be marked with 'AS'");
    }
}