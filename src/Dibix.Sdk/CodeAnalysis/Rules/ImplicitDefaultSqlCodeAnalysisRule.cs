using System.Collections.Generic;
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

        protected override void Visit(Table table)
        {
            HashSet<string> validColumns = new HashSet<string>(base.GetConstraints(table.Name)
                                                                   .Where(x => x.Type == ConstraintType.Nullable)
                                                                   .SelectMany(x => x.Columns.Select(y => y.Name)));

            foreach (ColumnDefinition column in table.Definition.ColumnDefinitions)
            {
                string columnName = column.ColumnIdentifier.Value;
                if (column.IsPersisted || validColumns.Contains(columnName))
                    continue;

                base.Fail(column, $"Please specify a nullable constraint for the column '{table.Name.BaseIdentifier.Value}.{columnName}' and don't rely on the default");
            }
        }
    }
}