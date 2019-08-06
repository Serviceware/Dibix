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
            ICollection<Constraint> constraints = base.GetConstraints(table.Name).ToArray();
            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            ICollection<string> nullableConstraintColumns = constraints.Where(x => x.Type == ConstraintType.Nullable)
                                                                       .SelectMany(x => x.Columns.Select(y => y.Name))
                                                                       .Distinct()
                                                                       .ToList();

            foreach (ColumnDefinition column in table.Definition.ColumnDefinitions.Where(x => x.IsPersisted))
                nullableConstraintColumns.Add(column.ColumnIdentifier.Value);

            foreach (ColumnReference column in primaryKey.Columns.Where(x => !nullableConstraintColumns.Contains(x.Name)))
            {
                base.Fail(column.Hit, $"Column must be explcitly marked as NOT NULL, since it is part of the primary key: {table.Name.BaseIdentifier.Value}.{column.Name}");
            }
        }
    }
}