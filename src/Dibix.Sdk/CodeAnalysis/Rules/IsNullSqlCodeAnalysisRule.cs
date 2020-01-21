using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class IsNullSqlCodeAnalysisRule : SqlCodeAnalysisRule<IsNullSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 34;
        public override string ErrorMessage => "Nullable columns in expressions should be wrapped with ISNULL(column, default value): {0}.{1}";
    }

    public sealed class IsNullSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            foreach (Constraint constraint in this.Model.GetConstraints(node.SchemaObjectName))
            {
                if (constraint.Kind != ConstraintKind.Check)
                    continue;

                TSqlFragment target = node.FindChild(constraint.Source);
                IDictionary<string, Column> dependentColumns = constraint.Columns.ToDictionary(x => x.Name);
                IsNotNullVisitor isNotNullVisitor = new IsNotNullVisitor();
                target.Accept(isNotNullVisitor);
                BooleanComparisonVisitor booleanComparisonVisitor = new BooleanComparisonVisitor();
                target.Accept(booleanComparisonVisitor);
                foreach (ColumnReferenceExpression columnReference in booleanComparisonVisitor.PlainColumns)
                {
                    string columnName = columnReference.GetName().Value;
                    if (!dependentColumns.TryGetValue(columnName, out Column column))
                        throw new InvalidOperationException($"Could not find column in check expression model: {constraint.Name} ({columnName})");

                    if (column.IsComputed || !column.IsNullable)
                        continue;

                    // We skip columns that are already checked for NULL in some way,
                    // because it's too complex to resolve the result of the condition
                    if (isNotNullVisitor.CheckedColumns.Contains(columnName))
                        continue;

                    base.Fail(columnReference, node.SchemaObjectName.BaseIdentifier.Value, columnName);
                }
            }
        }

        private sealed class IsNotNullVisitor : TSqlFragmentVisitor
        {
            public ICollection<string> CheckedColumns { get; }

            public IsNotNullVisitor()
            {
                this.CheckedColumns = new HashSet<string>();
            }

            public override void Visit(BooleanIsNullExpression node)
            {
                if (node.Expression is ColumnReferenceExpression column)
                    this.CheckedColumns.Add(column.GetName().Value);
            }
        }

        private sealed class BooleanComparisonVisitor : TSqlFragmentVisitor
        {
            public ICollection<ColumnReferenceExpression> PlainColumns { get; }

            public BooleanComparisonVisitor()
            {
                this.PlainColumns = new Collection<ColumnReferenceExpression>();
            }

            public override void Visit(BooleanComparisonExpression node)
            {
                if (node.FirstExpression is ColumnReferenceExpression leftColumnReference)
                    this.PlainColumns.Add(leftColumnReference);

                if (node.SecondExpression is ColumnReferenceExpression rightColumnReference)
                    this.PlainColumns.Add(rightColumnReference);
            }
        }
    }
}