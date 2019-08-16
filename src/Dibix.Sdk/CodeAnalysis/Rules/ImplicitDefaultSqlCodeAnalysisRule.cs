using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ImplicitDefaultSqlCodeAnalysisRule : SqlCodeAnalysisRule<ImplicitDefaultSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 25;
        public override string ErrorMessage => "{0}";
    }

    public sealed class ImplicitDefaultSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateIndexStatement node)
        {
            if (!node.Clustered.HasValue)
                base.Fail(node, $"Please specify the clustering (CLUSTERED/NONCLUSTERED) for the index '{node.Name.Value}' and don't rely on the default");
        }

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            foreach (ColumnDefinition column in tableDefinition.ColumnDefinitions)
            {
                if (column.IsPersisted || column.Constraints.OfType<NullableConstraintDefinition>().Any())
                    continue;

                string columnName = column.ColumnIdentifier.Value;
                base.Fail(column, $"Please specify a nullable constraint for the column '{tableName.BaseIdentifier.Value}.{columnName}' and don't rely on the default");
            }
        }
    }
}