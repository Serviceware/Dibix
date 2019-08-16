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
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Check(node.SchemaObjectName, nameof(NamingConventions.Table), NamingConventions.Table);
            base.Visit(node);
        }

        // Views
        public override void Visit(CreateViewStatement node) => this.Check(node.SchemaObjectName, nameof(NamingConventions.View), NamingConventions.View);

        // UDTs
        public override void Visit(CreateTypeStatement node) => this.Check(node.Name, nameof(NamingConventions.Type), NamingConventions.Type);

        // Sequences
        public override void Visit(CreateSequenceStatement node) => this.Check(node.Name, nameof(NamingConventions.Sequence), NamingConventions.Sequence);

        // Stored procedures
        public override void Visit(CreateProcedureStatement node) => this.Check(node.ProcedureReference.Name, nameof(NamingConventions.Procedure), NamingConventions.Procedure);

        // Functions
        public override void Visit(CreateFunctionStatement node) => this.Check(node.Name, nameof(NamingConventions.Function), NamingConventions.Function);

        // Table columns
        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            // Columns
            foreach (ColumnDefinition column in tableDefinition.ColumnDefinitions)
            {
                if (ColumnWorkarounds.Contains($"{tableName.BaseIdentifier.Value}#{column.ColumnIdentifier.Value}"))
                    continue;

                if (!Regex.IsMatch(column.ColumnIdentifier.Value, NamingConventions.Column.Pattern))
                    base.Fail(column, $"Column names should only contain the characters 'a-z_' and have no trailing underscores: {tableName.BaseIdentifier.Value}.{column.ColumnIdentifier.Value}");
            }

            this.VisitConstraints(tableModel, tableName);
            this.VisitIndexes(tableModel, tableName);
        }

        // Constraints
        private void VisitConstraints(TableModel tableModel, SchemaObjectName tableName)
        {
            foreach (Constraint constraint in base.Model.GetConstraints(tableModel, tableName))
            {
                if (constraint.Kind == ConstraintKind.Nullable)
                    return;

                if (constraint.Name == null)
                    return;

                NamingConvention namingConvention = GetNamingConvention(constraint.Kind);
                this.Check(constraint.Definition.ConstraintIdentifier, constraint.Name, constraint.KindDisplayName, namingConvention, ResolveConstraintPlaceholders(tableName.BaseIdentifier.Value, constraint.Columns));
            }
        }

        // Indexes
        private void VisitIndexes(TableModel tableModel, SchemaObjectName tableName)
        {
            foreach (Index index in base.Model.GetIndexes(tableModel, tableName))
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
                this.Check(index.Identifier, index.Name, displayName, namingConvention, ResolveConstraintPlaceholders(tableName.BaseIdentifier.Value, index.Columns));
            }
        }

        private void Check(SchemaObjectName schemaObjectName, string displayName, NamingConvention namingConvention, params KeyValuePair<string, string>[] replacements) => this.Check(schemaObjectName.BaseIdentifier, displayName, namingConvention, replacements.AsEnumerable());
        private void Check(Identifier identifier, string displayName, NamingConvention namingConvention, IEnumerable<KeyValuePair<string, string>> replacements) => this.Check(identifier, identifier.Value, displayName, namingConvention, replacements.AsEnumerable());
        private void Check(TSqlFragment target, string name, string displayName, NamingConvention namingConvention, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            if (Workarounds.Contains(name))
                return;

            string mask = BuildMask(namingConvention.Pattern, replacements.ToDictionary(x => x.Key, x => x.Value));
            if (!Regex.IsMatch(name, mask))
                base.Fail(target, $"{displayName} '{name}' does not match naming convention '{namingConvention.Description}'. Also make sure the name is all lowercase.");
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

        private static NamingConvention GetNamingConvention(ConstraintKind constraintKind)
        {
            switch (constraintKind)
            {
                case ConstraintKind.PrimaryKey: return NamingConventions.PrimaryKeyConstraint;
                case ConstraintKind.ForeignKey: return NamingConventions.ForeignKeyConstraint;
                case ConstraintKind.Unique: return NamingConventions.UniqueConstraint;
                case ConstraintKind.Check: return NamingConventions.CheckConstraint;
                case ConstraintKind.Default: return NamingConventions.DefaultConstraint;
                default: throw new ArgumentOutOfRangeException(nameof(constraintKind), constraintKind, null);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ResolveConstraintPlaceholders(string tableName, IList<Column> columns)
        {
            yield return new KeyValuePair<string, string>("tablename", tableName);
            //yield return new KeyValuePair<string, string>("columnnames", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)));

            if (columns.Count == 1)
                yield return new KeyValuePair<string, string>("columnname", columns[0].Name);
        }
    }
}