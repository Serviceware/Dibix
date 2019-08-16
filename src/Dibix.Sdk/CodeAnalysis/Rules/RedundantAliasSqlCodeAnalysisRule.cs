using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // This rule is disabled because you might want to know all column names that are returned
    // For example:
    // [id]   AS [id]
    // [name] AS [name]
    // 
    // [id]   = [id]
    // [name] = [name]
    public sealed class RedundantAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<RedundantAliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 6;
        public override string ErrorMessage => "The alias is redundant";
        public override bool IsEnabled => false;
    }

    public sealed class RedundantAliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SelectScalarExpression node)
        {
            if (!(node.Expression is ColumnReferenceExpression columnReference) || node.ColumnName == null)
                return;

            string alias = node.ColumnName.Value;
            string columnName = columnReference.GetName().Value;
            if (alias == columnName)
                base.Fail(node.ColumnName);
        }
    }
}
