using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 29)]
    public sealed class TableConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "The constraint '{0}' should be defined on the table, instead of the column '{1}'";

        public TableConstraintSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateTableStatement node)
        {
            foreach (ColumnDefinition column in node.Definition.ColumnDefinitions)
            {
                foreach (ConstraintDefinition constraint in column.Constraints)
                {
                    if (constraint is UniqueConstraintDefinition unique && unique.Columns.Any()
                     || constraint is ForeignKeyConstraintDefinition foreignKey && foreignKey.Columns.Any())
                    {
                        base.Fail(constraint, constraint.ConstraintIdentifier.Value, column.ColumnIdentifier.Value);
                    }
                }
            }
        }
    }
}
 