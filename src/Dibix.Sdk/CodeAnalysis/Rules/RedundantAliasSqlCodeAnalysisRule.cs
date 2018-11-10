using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class RedundantAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<RedundantAliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 6;
        public override string ErrorMessage => "The alias is redundant";
    }

    public sealed class RedundantAliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SelectScalarExpression node)
        {
            if (!(node.Expression is ColumnReferenceExpression columnReference) || node.ColumnName == null)
                return;

            string alias = node.ColumnName.Value;
            string columnName = columnReference.MultiPartIdentifier?.Identifiers.Last().Value;
            if (alias == columnName)
                base.Fail(node.ColumnName);
        }
    }
}