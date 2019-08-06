using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class NamingConventionSqlCodeAnalysisRule : SqlCodeAnalysisRule<NamingConventionSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 17;
        public override string ErrorMessage => "{0}";
    }

    internal static class NamingConventions
    {
#if HELPLINE
        private const string Prefix = "hl";
#else
        private const string Prefix = "dbx";
#endif
        private const string TextRegex = "[a-z0-9_]+";

        public static readonly NamingConvention Table                = new NamingConvention($"^{Prefix}{TextRegex}$",                 $"{Prefix}*");
        public static readonly NamingConvention View                 = new NamingConvention($"^{Prefix}{TextRegex}vw$",               $"{Prefix}*vw");
        public static readonly NamingConvention Type                 = new NamingConvention($"^{Prefix}{TextRegex}_udt_{TextRegex}$", $"{Prefix}*_udt_*");
        public static readonly NamingConvention Sequence             = new NamingConvention($"^SEQ_{Prefix}{TextRegex}$",             $"SEQ_{Prefix}*");
        public static readonly NamingConvention Procedure            = new NamingConvention($"^{Prefix}{TextRegex}$",                 $"{Prefix}*");
        public static readonly NamingConvention Function             = new NamingConvention($"^{Prefix}{TextRegex}$",                 $"{Prefix}*");
        public static readonly NamingConvention PrimaryKeyConstraint = new NamingConvention("^PK_<tablename>$",                       "PK_<tablename>");
      //public static readonly NamingConvention ForeignKeyConstraint = new NamingConvention("^FK_<tablename>_<columnnames>$",         "FK_<tablename>_<columnnames>");
        public static readonly NamingConvention ForeignKeyConstraint = new NamingConvention($"^FK_<tablename>_{TextRegex}$",          "FK_<tablename>_*");
        public static readonly NamingConvention CheckConstraint      = new NamingConvention($"^CK_<tablename>_{TextRegex}$",          "CK_<tablename>_*");
      //public static readonly NamingConvention UniqueConstraint     = new NamingConvention($"^UQ_<tablename>_<columnnames>$",        "UQ_<tablename>_<columnnames>");
        public static readonly NamingConvention UniqueConstraint     = new NamingConvention($"^UQ_<tablename>_{TextRegex}$",          "UQ_<tablename>_*");
        public static readonly NamingConvention DefaultConstraint    = new NamingConvention("^DF_<tablename>_<columnname>$",          "DF_<tablename>_<columnname>");
        public static readonly NamingConvention Index                = new NamingConvention($"^IX_<tablename>_{TextRegex}$",          "IX_<tablename>_*");
        public static readonly NamingConvention UniqueIndex          = new NamingConvention($"^UQ_<tablename>_{TextRegex}$",          "UQ_<tablename>_*");
        public static readonly NamingConvention Column               = new NamingConvention("^[a-z](([a-z_]+)?[a-z])?$");
    }

    internal struct NamingConvention
    {
        public string Pattern { get; set; }
        public string Description { get; set; }

        public NamingConvention(string pattern) : this(pattern, pattern) { }
        public NamingConvention(string pattern, string description)
        {
            this.Pattern = pattern;
            this.Description = description;
        }
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
        private static readonly HashSet<string> ColumnWorkarounds = new HashSet<string>
        {
            "BlobDetail#blobmeta_identifier"
          , "hlsysbaselineattr#fixedvalue2"
          , "hlsysportalconfig#attrpathapp1"
          , "hlsysportalconfig#attrpathapp2"
          , "hlsysportalconfig#attrpathacc1"
          , "hlsysportalconfig#attrpathacc2"
          , "hlsysportalconfig#attrpathpassword1"
          , "hlsysportalconfig#attrpathpassword2"
          , "hlsysslmservicehoursentry#time1"
          , "hlsysslmservicehoursentry#time2"
          , "hlsysslmservicehoursentry#dayofweek1"
          , "hlsysslmservicehoursentry#dayofweek2"
          , "hlsysslmservicehoursentry#datetime1"
          , "hlsysslmservicehoursentry#datetime2"
        };

        // Tables
        public override void Visit(CreateTableStatement node) => this.Check(node.SchemaObjectName.BaseIdentifier, nameof(NamingConventions.Table), NamingConventions.Table);

        // Views
        public override void Visit(CreateViewStatement node) => this.Check(node.SchemaObjectName.BaseIdentifier, nameof(NamingConventions.View), NamingConventions.View);

        // UDTs
        public override void Visit(CreateTypeStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConventions.Type), NamingConventions.Type);

        // Sequences
        public override void Visit(CreateSequenceStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConventions.Sequence), NamingConventions.Sequence);
        
        // Stored procedures
        public override void Visit(CreateProcedureStatement node) => this.Check(node.ProcedureReference.Name.BaseIdentifier, nameof(NamingConventions.Procedure), NamingConventions.Procedure);

        // Functions
        public override void Visit(CreateFunctionStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConventions.Function), NamingConventions.Function);

        // Table columns
        protected override void Visit(Table table)
        {
            // Columns
            foreach (ColumnDefinition column in table.Definition.ColumnDefinitions)
            {
                if (ColumnWorkarounds.Contains($"{table.Name.BaseIdentifier.Value}#{column.ColumnIdentifier.Value}"))
                    continue;

                if (!Regex.IsMatch(column.ColumnIdentifier.Value, NamingConventions.Column.Pattern))
                    base.Fail(column, $"Column names should only contain the characters 'a-z_' and have no trailing underscores: {table.Name.BaseIdentifier.Value}.{column.ColumnIdentifier.Value}");
            }
        }

        // Constraints
        protected override void Visit(ConstraintTarget target, Constraint constraint)
        {
            Identifier identifier = constraint.Definition.ConstraintIdentifier;
            if (constraint.Type == ConstraintType.Nullable)
                return;

            if (identifier == null)
                return;

            NamingConvention namingConvention = GetNamingConvention(constraint.Type);
            this.Check(identifier, constraint.TypeDisplayName, namingConvention, ResolveConstraintPlaceholders(target.Name, constraint.Columns));
        }

        // Indexes
        protected override void Visit(IndexTarget target, Index index)
        {
            string displayName;
            NamingConvention namingConvention;
            if (index.IsUnique)
            {
                displayName = "Unique index";
                namingConvention = NamingConventions.UniqueIndex;
            }
            else
            {
                displayName = nameof(NamingConventions.Index);
                namingConvention = NamingConventions.Index;
            }
            this.Check(index.Identifier, displayName, namingConvention, ResolveConstraintPlaceholders(target.Name, index.Columns));
        }

        private void Check(Identifier identifier, string displayName, NamingConvention namingConvention, params KeyValuePair<string, string>[] replacements) => this.Check(identifier, displayName, namingConvention, replacements.AsEnumerable());
        private void Check(Identifier identifier, string displayName, NamingConvention namingConvention, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            if (Workarounds.Contains(identifier.Value))
                return;

            string mask = BuildMask(namingConvention.Pattern, replacements.ToDictionary(x => x.Key, x => x.Value));
            if (!Regex.IsMatch(identifier.Value, mask))
                base.Fail(identifier, $"{displayName} '{identifier.Value}' does not match naming convention '{namingConvention.Description}'. Also make sure the name is all lowercase.");
        }

        private static string BuildMask(string pattern, IDictionary<string, string> replacements)
        {
            string OnMatch(Match match)
            {
                return replacements.TryGetValue(match.Value.TrimStart('<').TrimEnd('>'), out string replacement) ? replacement : match.Value;
            }

            string replacementPattern = String.Join("|", new[] { @"\*" }.Concat(replacements.Keys.Select(x => $@"\<{x}\>")));
            string mask = Regex.Replace(pattern, replacementPattern, OnMatch);
            return mask;
        }

        private static NamingConvention GetNamingConvention(ConstraintType constraintType)
        {
            switch (constraintType)
            {
                case ConstraintType.PrimaryKey: return NamingConventions.PrimaryKeyConstraint;
                case ConstraintType.ForeignKey: return NamingConventions.ForeignKeyConstraint;
                case ConstraintType.Unique: return NamingConventions.UniqueConstraint;
                case ConstraintType.Check: return NamingConventions.CheckConstraint;
                case ConstraintType.Default: return NamingConventions.DefaultConstraint;
                default: throw new ArgumentOutOfRangeException(nameof(constraintType), constraintType, null);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ResolveConstraintPlaceholders(SchemaObjectName targetName, IList<ColumnReference> columns)
        {
            yield return new KeyValuePair<string, string>("tablename", targetName.BaseIdentifier.Value);
            //yield return new KeyValuePair<string, string>("columnnames", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)));

            if (columns.Count == 1)
                yield return new KeyValuePair<string, string>("columnname", columns[0].Name);
        }
    }
}