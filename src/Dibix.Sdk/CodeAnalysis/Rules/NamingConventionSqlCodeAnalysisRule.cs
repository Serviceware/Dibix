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
        public override string ErrorMessage => "{0} '{1}' does not match naming convention '{2}'. Make sure the name is all lowercase.";
    }

    internal static class NamingConvention
    {
        public static readonly string Table                = "hl*";
        public static readonly string View                 = "hl*";
        public static readonly string Type                 = "hl*";
        public static readonly string Sequence             = "SEQ_hl*";
        public static readonly string Procedure            = "hl*";
        public static readonly string Function             = "hl*";
        public static readonly string PrimaryKeyConstraint = "PK_<tablename>";
      //public static readonly string ForeignKeyConstraint = "FK_<tablename>_<columnnames>";
        public static readonly string ForeignKeyConstraint = "FK_<tablename>_*";
        public static readonly string CheckConstraint      = "CK_<tablename>_*";
      //public static readonly string UniqueConstraint     = "UQ_<tablename>_<columnnames>";
        public static readonly string UniqueConstraint     = "UQ_<tablename>_*";
        public static readonly string DefaultConstraint    = "DF_<tablename>_<columnname>";
    }

    public sealed class NamingConventionSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_hlsysobjecdata" // Renaming this PK would force rebuilding the full text catalog which would be very slow
            
            // Temporary helpLine common blob component workarounds
          , "Blob"
          , "BlobDetail"
          , "FK_BlobDetail_BlobMetaIdentifier"
          , "fnSplit"
          , "spBlobSelect"
          , "spBlobTextSearch"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (!node.IsTemporaryTable())
                this.Check(node.SchemaObjectName.BaseIdentifier, "Table", NamingConvention.Table);

            IEnumerable<Constraint> constraints = node.Definition.CollectConstraints();
            foreach (Constraint constraint in constraints)
            {
                this.VisitConstraint(node, constraint);
            }
        }

        public override void Visit(CreateViewStatement node) => this.Check(node.SchemaObjectName.BaseIdentifier, "View", NamingConvention.View);

        public override void Visit(CreateTypeStatement node) => this.Check(node.Name.BaseIdentifier, "Type", NamingConvention.Type);

        public override void Visit(CreateSequenceStatement node) => this.Check(node.Name.BaseIdentifier, "Sequence", NamingConvention.Sequence);

        public override void Visit(CreateProcedureStatement node) => this.Check(node.ProcedureReference.Name.BaseIdentifier, "Procedure", NamingConvention.Procedure);

        public override void Visit(CreateFunctionStatement node) => this.Check(node.Name.BaseIdentifier, "Function", NamingConvention.Function);

        private void Check(Identifier identifier, string displayName, string pattern, params KeyValuePair<string, string>[] replacements) => this.Check(identifier, displayName, pattern, replacements.AsEnumerable());

        private void Check(Identifier identifier, string displayName, string pattern, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            if (Workarounds.Contains(identifier.Value))
                return;

            string mask = BuildMask(pattern, replacements.ToDictionary(x => x.Key, x => x.Value));
            if (!Regex.IsMatch(identifier.Value, mask))
                base.Fail(identifier, displayName, identifier.Value, pattern);
        }

        private static string BuildMask(string pattern, IDictionary<string, string> replacements)
        {
            string OnMatch(Match match)
            {
                if (match.Value == "*")
                    return "[a-z0-9_]+";

                return replacements.TryGetValue(match.Value.TrimStart('<').TrimEnd('>'), out string replacement) ? replacement : match.Value;
            }

            string replacementPattern = String.Join("|", new[] { @"\*" }.Concat(replacements.Keys.Select(x => $@"\<{x}\>")));
            string mask = $"^{Regex.Replace(pattern, replacementPattern, OnMatch)}$";
            return mask;
        }

        private void VisitConstraint(CreateTableStatement createTableStatement, Constraint constraint)
        {
            Identifier identifier = constraint.Definition.ConstraintIdentifier;
            if (constraint.Type == ConstraintType.Nullable)
                return;

            if (identifier == null)
                return;

            string pattern = GetNamingConvention(constraint.Type);
            this.Check(identifier, constraint.Type.ToDisplayName(), pattern, ResolveConstraintPlaceholders(createTableStatement, constraint.Columns));
        }

        private static string GetNamingConvention(ConstraintType constraintType)
        {
            switch (constraintType)
            {
                case ConstraintType.PrimaryKey: return NamingConvention.PrimaryKeyConstraint;
                case ConstraintType.ForeignKey: return NamingConvention.ForeignKeyConstraint;
                case ConstraintType.Unique: return NamingConvention.UniqueConstraint;
                case ConstraintType.Check: return NamingConvention.CheckConstraint;
                case ConstraintType.Default: return NamingConvention.DefaultConstraint;
                default: throw new ArgumentOutOfRangeException(nameof(constraintType), constraintType, null);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ResolveConstraintPlaceholders(CreateTableStatement table, IList<ColumnReference> columns)
        {
            yield return new KeyValuePair<string, string>("tablename", table.SchemaObjectName.BaseIdentifier.Value);
            yield return new KeyValuePair<string, string>("columnnames", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)));

            if (columns.Count == 1)
                yield return new KeyValuePair<string, string>("columnname", columns[0].Name);
        }
    }
}