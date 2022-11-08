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
    [SqlCodeAnalysisRule(id: 6, IsEnabled = false)]
    public sealed class RedundantAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "The alias is redundant";

        public RedundantAliasSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

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
