using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class NamingConventionSqlCodeAnalysisRule : SqlCodeAnalysisRule<NamingConventionSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 17;
        public override string ErrorMessage => "{0} '{1}' does not match naming convention '{2}'";
    }

    internal sealed class NamingConventions
    {
        private static readonly IDictionary<ConstraintType, string> Registrations = new Dictionary<ConstraintType, string>
        {
            { ConstraintType.PrimaryKey, "PK_%TABLENAME%" }
        //, { ConstraintType.ForeignKey, "FK_%TABLENAME%_%COLUMNNAMES%" }
          , { ConstraintType.ForeignKey, "FK_%TABLENAME%_*" }
          , { ConstraintType.Check,      "CK_%TABLENAME%_*" }
        //, { ConstraintType.Unique,     "UQ_%TABLENAME%_%COLUMNNAMES%" }
          , { ConstraintType.Unique,     "UQ_%TABLENAME%_*" }
          , { ConstraintType.Default,    "DF_%TABLENAME%_%COLUMNNAME%" }
        };

        public static string GetPattern(ConstraintType type) => Registrations[type];
    }

    public sealed class NamingConventionSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            IEnumerable<Constraint> constraints = CollectConstraints(node.Definition);

            foreach (Constraint constraint in constraints)
            {
                string pattern = NamingConventions.GetPattern(constraint.Type);
                string mask = BuildMask(node, pattern, constraint.ParentName);
                if (!Regex.IsMatch(constraint.Identifier.Value, mask))
                    base.Fail(constraint.Identifier, constraint.Type, constraint.Identifier.Value, pattern);
            }
        }

        private static IEnumerable<Constraint> CollectConstraints(TableDefinition table)
        {
            foreach (ConstraintDefinition constraint in table.TableConstraints)
                yield return Constraint.Create(constraint);

            foreach (ColumnDefinition column in table.ColumnDefinitions)
            {
                foreach (ConstraintDefinition constraint in column.Constraints)
                    yield return Constraint.Create(constraint);

                if (column.DefaultConstraint != null)
                    yield return Constraint.Create(column.DefaultConstraint, column.ColumnIdentifier.Value);
            }
        }

        private static string BuildMask(CreateTableStatement table, string pattern, string parentName)
        {
            string mask = pattern.Replace("%TABLENAME%", table.SchemaObjectName.BaseIdentifier.Value)
                                 .Replace("%COLUMNNAMES%", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)))
                                 .Replace("*", @"[^\W_]+");

            if (parentName != null)
                mask = mask.Replace("%COLUMNNAME%", parentName);

            mask = $"^{mask}$";
            return mask;
        }

        private class Constraint
        {
            public Identifier Identifier { get; }
            public ConstraintType Type { get; }
            public string ParentName { get; }

            private Constraint(Identifier identifier, ConstraintType type, string parentName)
            {
                this.Identifier = identifier;
                this.Type = type;
                this.ParentName = parentName;
            }

            public static Constraint Create(ConstraintDefinition definition, string parentName = null)
            {
                ConstraintType type = DetermineConstraintType(definition);
                return new Constraint(definition.ConstraintIdentifier, type, parentName);
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
                }
                return ConstraintType.Unknown;
            }
        }
    }

    internal enum ConstraintType
    {
        Unknown,
        PrimaryKey,
        ForeignKey,
        Unique,
        Check,
        Default
    }
}