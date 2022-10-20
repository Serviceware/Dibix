using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 31)]
    public sealed class UnintentionalBooleanComparisonSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Unintentional boolean comparison: {0}";

        public UnintentionalBooleanComparisonSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(BooleanComparisonExpression node)
        {
            if (node.FirstExpression.Dump() == node.SecondExpression.Dump() // @a = @a
             || node.FirstExpression is Literal && node.SecondExpression is Literal) // 1 = 1 or 1 = 2
                base.Fail(node, node.Dump());
        }

        public override void Visit(BooleanIsNullExpression node)
        {
             if (node.Expression is Literal) // 1 IS NOT NULL
                base.Fail(node, node.Dump());
        }
    }
}