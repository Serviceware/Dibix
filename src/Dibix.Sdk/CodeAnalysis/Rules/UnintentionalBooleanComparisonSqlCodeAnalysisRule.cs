using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnintentionalBooleanComparisonSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnintentionalBooleanComparisonSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 31;
        public override string ErrorMessage => "Unintentional boolean comparison: {0}";
    }

    public sealed class UnintentionalBooleanComparisonSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
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