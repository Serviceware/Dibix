using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Check(node.SchemaObjectName, node.Definition);
        }

        public override void Visit(CreateTypeTableStatement node) => this.Check(node.Name, node.Definition);

        private void Check(SchemaObjectName name, TableDefinition definition)
        {
            ICollection<Constraint> constraints = definition.CollectConstraints().ToArray();
            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            ICollection<string> nullableConstraintColumns = constraints.Where(x => x.Type == ConstraintType.Nullable)
                                                                       .SelectMany(x => x.Columns.Select(y => y.Name))
                                                                       .Distinct()
                                                                       .ToArray();

            foreach (ColumnReference column in primaryKey.Columns.Where(x => !nullableConstraintColumns.Contains(x.Name)))
            {
                base.Fail(column.Hit, $"Column must be explcitly marked as NOT NULL, since it is part of the primary key: {name.BaseIdentifier.Value}.{column.Name}");
            }
        }
    }
}
 