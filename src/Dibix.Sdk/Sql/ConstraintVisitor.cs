using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public class ConstraintVisitor : TableDefinitionVisitor
    {
        public IDictionary<string, ConstraintTarget> Targets { get; }

        public ConstraintVisitor()
        {
            this.Targets = new Dictionary<string, ConstraintTarget>();
        }

        protected override void Visit(Table table)
        {
            string key = table.Name.ToKey();
            if (!this.Targets.TryGetValue(key, out ConstraintTarget target))
            {
                target = new ConstraintTarget(table.Name);
                this.Targets.Add(key, target);
            }
            target.Constraints.AddRange(CollectConstraints(table.Definition));
        }

        private static IEnumerable<Constraint> CollectConstraints(TableDefinition table)
        {
            foreach (ColumnDefinition column in table.ColumnDefinitions)
            {
                foreach (ConstraintDefinition constraint in column.Constraints)
                    yield return Create(constraint, new ColumnReference(column.ColumnIdentifier.Value, column));

                if (column.DefaultConstraint != null)
                    yield return Create(column.DefaultConstraint, new ColumnReference(column.ColumnIdentifier.Value, column));
            }

            foreach (ConstraintDefinition constraint in table.TableConstraints)
                yield return Create(constraint);
        }

        private static Constraint Create(ConstraintDefinition definition, ColumnReference column = null)
        {
            ConstraintType type;
            ICollection<ColumnReference> columns = new Collection<ColumnReference>();

            switch (definition)
            {
                case UniqueConstraintDefinition uniqueConstraint:
                    type = uniqueConstraint.IsPrimaryKey ? ConstraintType.PrimaryKey : ConstraintType.Unique;
                    columns.AddRange(uniqueConstraint.Columns.Select(x => new ColumnReference(x.Column.MultiPartIdentifier.Identifiers.Last().Value, x)));
                    break;

                case ForeignKeyConstraintDefinition foreignKeyConstraint:
                    type = ConstraintType.ForeignKey;
                    columns.AddRange(foreignKeyConstraint.Columns.Select(x => new ColumnReference(x.Value, x)));
                    break;

                case CheckConstraintDefinition _:
                    type = ConstraintType.Check;
                    break;

                case DefaultConstraintDefinition defaultConstraint:
                    type = ConstraintType.Default;
                    if (defaultConstraint.Column != null)
                        columns.Add(new ColumnReference(defaultConstraint.Column.Value, defaultConstraint.Column));

                    break;

                case NullableConstraintDefinition nullableConstraint when nullableConstraint.ConstraintIdentifier != null:
                    throw new NotSupportedException($"Nullable constraints cannot have a name, can they? [{nullableConstraint.ConstraintIdentifier.Dump()}]");

                case NullableConstraintDefinition _:
                    type = ConstraintType.Nullable;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(definition), definition, null);
            }

            if (!columns.Any() && column != null)
                columns.Add(column);

            return new Constraint(definition, type, columns);
        }
    }
}