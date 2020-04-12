using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 8)]
    public sealed class ImplicitAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Aliases must be marked with 'AS'";
        
        public override void Visit(TableReferenceWithAlias node)
        {
            if (node.Alias == null)
                return;

            for (int i = node.Alias.FirstTokenIndex; i > node.FirstTokenIndex; i--)
            {
                if (node.ScriptTokenStream[i].TokenType == SqlTokenType.As)
                    return;
            }
            base.Fail(node.Alias);
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
            base.Fail(node.ColumnName);
        }
    }
}