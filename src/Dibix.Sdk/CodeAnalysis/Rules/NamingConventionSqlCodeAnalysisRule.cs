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
            IEnumerable<Constraint> constraints = node.Definition
                                                      .CollectConstraints()
                                                      .Where(x => x.Type != ConstraintType.Nullable && x.Definition.ConstraintIdentifier != null);

            foreach (Constraint constraint in constraints)
            {
                string pattern = NamingConventions.GetPattern(constraint.Type);
                string mask = BuildMask(node, pattern, constraint.ParentName);
                if (!Regex.IsMatch(constraint.Definition.ConstraintIdentifier.Value, mask))
                    base.Fail(constraint.Definition.ConstraintIdentifier, constraint.Type.ToDisplayName(), constraint.Definition.ConstraintIdentifier.Value, pattern);
            }
        }

        private static string BuildMask(CreateTableStatement table, string pattern, string parentName)
        {
            string mask = pattern.Replace("%TABLENAME%", table.SchemaObjectName.BaseIdentifier.Value)
                                 .Replace("%COLUMNNAMES%", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)))
                                 .Replace("*", @"\w+");

            if (parentName != null)
                mask = mask.Replace("%COLUMNNAME%", parentName);

            mask = $"^{mask}$";
            return mask;
        }
    }
}