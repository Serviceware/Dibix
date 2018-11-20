using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    internal sealed class Constraint
    {
        public ConstraintDefinition Definition { get; }
        public ConstraintType Type { get; }
        public string ParentName { get; }

        private Constraint(ConstraintDefinition definition, ConstraintType type, string parentName)
        {
            this.Definition = definition;
            this.Type = type;
            this.ParentName = parentName;
        }

        public static Constraint Create(ConstraintDefinition definition, string parentName = null)
        {
            ConstraintType type = DetermineConstraintType(definition);
            return new Constraint(definition, type, parentName);
        }

        private static ConstraintType DetermineConstraintType(ConstraintDefinition constraint)
        {
            switch (constraint)
            {
                case UniqueConstraintDefinition uniqueConstraint: return uniqueConstraint.IsPrimaryKey ? ConstraintType.PrimaryKey : ConstraintType.Unique;
                case ForeignKeyConstraintDefinition _: return ConstraintType.ForeignKey;
                case CheckConstraintDefinition _: return ConstraintType.Check;
                case DefaultConstraintDefinition _: return ConstraintType.Default;
                case NullableConstraintDefinition nullableConstraint when nullableConstraint.ConstraintIdentifier != null:
                    throw new NotSupportedException($"Nullable constraints cannot have a name, can they? [{nullableConstraint.ConstraintIdentifier.Dump()}]");
                case NullableConstraintDefinition _: return ConstraintType.Nullable;
                default: throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null);
            }
        }
    }
}