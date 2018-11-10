using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Lint.Rules
{
    public sealed class SqlRedundantAliasLintRule : SqlLintRule
    {
        public override int Id => 6;
        public override string ErrorMessage => "The alias is redundant";

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