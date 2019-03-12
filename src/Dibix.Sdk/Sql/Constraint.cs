using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal sealed class Constraint
    {
        public ConstraintDefinition Definition { get; }
        public ConstraintType Type { get; }
        public IList<ColumnReference> Columns { get; }

        private Constraint(ConstraintDefinition definition, ConstraintType type, IEnumerable<ColumnReference> columns)
        {
            this.Definition = definition;
            this.Type = type;
            this.Columns = new Collection<ColumnReference>();
            this.Columns.AddRange(columns);
        }

        public static Constraint Create(ConstraintDefinition definition, ColumnReference column = null)
        {
            ConstraintType type;
            ICollection<ColumnReference> columns = new Collection<ColumnReference>();
            if (column != null)
                columns.Add(column);

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
            return new Constraint(definition, type, columns);
        }
    }
}