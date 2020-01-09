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
        private const string TextRegex = "[a-z0-9_]+";

        public static readonly NamingConvention Table                = new NamingConvention($"^{Placeholder.Prefix}{TextRegex}$",                 $"{Placeholder.Prefix}*");
        public static readonly NamingConvention View                 = new NamingConvention($"^{Placeholder.Prefix}{TextRegex}vw$",               $"{Placeholder.Prefix}*vw");
        public static readonly NamingConvention Type                 = new NamingConvention($"^{Placeholder.Prefix}{TextRegex}_udt_{TextRegex}$", $"{Placeholder.Prefix}*_udt_*");
        public static readonly NamingConvention Sequence             = new NamingConvention($"^SEQ_{Placeholder.Prefix}{TextRegex}$",             $"SEQ_{Placeholder.Prefix}*");
        public static readonly NamingConvention Procedure            = new NamingConvention($"^{Placeholder.Prefix}{TextRegex}$",                 $"{Placeholder.Prefix}*");
        public static readonly NamingConvention Function             = new NamingConvention($"^{Placeholder.Prefix}{TextRegex}$",                 $"{Placeholder.Prefix}*");
        public static readonly NamingConvention PrimaryKeyConstraint = new NamingConvention($"^PK_{Placeholder.Table}$",                          $"PK_{Placeholder.Table}");
        public static readonly NamingConvention ForeignKeyConstraint = new NamingConvention($"^FK_{Placeholder.Table}_{TextRegex}$",              $"FK_{Placeholder.Table}_*");
        public static readonly NamingConvention CheckConstraint      = new NamingConvention($"^CK_{Placeholder.Table}_{TextRegex}$",              $"CK_{Placeholder.Table}_*");
        public static readonly NamingConvention UniqueConstraint     = new NamingConvention($"^UQ_{Placeholder.Table}_{TextRegex}$",              $"UQ_{Placeholder.Table}_*");
        public static readonly NamingConvention DefaultConstraint    = new NamingConvention($"^DF_{Placeholder.Table}_{Placeholder.Column}$",     $"DF_{Placeholder.Table}_{Placeholder.Column}");
        public static readonly NamingConvention Index                = new NamingConvention($"^IX_{Placeholder.Table}_{TextRegex}$",              $"IX_{Placeholder.Table}_*");
        public static readonly NamingConvention UniqueIndex          = new NamingConvention($"^UQ_{Placeholder.Table}_{TextRegex}$",              $"UQ_{Placeholder.Table}_*");
        public static readonly NamingConvention Column               = new NamingConvention("^[a-z](([a-z_]+)?[a-z])?$");
    }

    public sealed class NamingConventionSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly ICollection<string> Workarounds = new HashSet<string>
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
        private static readonly ICollection<string> ColumnWorkarounds = new HashSet<string>
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
                this.Check(constraint.Definition.ConstraintIdentifier, constraint.Name, constraint.KindDisplayName, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, constraint.Columns));
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
                this.Check(index.Identifier, index.Name, displayName, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, index.Columns));
            }
        }

        private void Check(SchemaObjectName schemaObjectName, string displayName, NamingConvention namingConvention) => this.Check(schemaObjectName.BaseIdentifier, displayName, namingConvention, null);
        private void Check(Identifier identifier, string displayName, NamingConvention namingConvention, Action<PatternNormalizer> replacements) => this.Check(identifier, identifier.Value, displayName, namingConvention, replacements);
        private void Check(TSqlFragment target, string name, string displayName, NamingConvention namingConvention, Action<PatternNormalizer> replacements)
        {
            if (Workarounds.Contains(name))
                return;

            string mask = namingConvention.NormalizePattern(base.Configuration, replacements);
            string description = namingConvention.NormalizeDescription(this.Configuration);
            if (!Regex.IsMatch(name, mask))
            {
                base.Fail(target, $"{displayName} '{name}' does not match naming convention '{description}'. Also make sure the name is all lowercase.");
            }
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

        private static void ResolveConstraintPlaceholders(PatternNormalizer normalizer, string tableName, IList<Column> columns)
        {
            normalizer.ResolvePlaceholder(Placeholder.Table.Name, tableName);

            if (columns.Count == 1)
                normalizer.ResolvePlaceholder(Placeholder.Column.Name, columns[0].Name);
        }
    }

    internal struct NamingConvention
    {
        public string Pattern { get; }
        public string Description { get; }

        public NamingConvention(string pattern) : this(pattern, pattern) { }
        public NamingConvention(string pattern, string description)
        {
            this.Pattern = pattern;
            this.Description = description;
        }

        public string NormalizePattern(SqlCodeAnalysisConfiguration configuration, Action<PatternNormalizer> replacements)
        {
            PatternNormalizer normalizer = new PatternNormalizer(this.Pattern, configuration);
            replacements?.Invoke(normalizer);
            return normalizer.Normalize();
        }

        public string NormalizeDescription(SqlCodeAnalysisConfiguration configuration)
        {
            PatternNormalizer normalizer = new PatternNormalizer(this.Description, configuration);
            return normalizer.Normalize();
        }
    }

    internal struct Placeholder
    {
        public static readonly Placeholder Prefix = new Placeholder("prefix", true);
        public static readonly Placeholder Table  = new Placeholder("table");
        public static readonly Placeholder Column = new Placeholder("column");

        public string Name { get; }
        public bool NormalizeDescription { get; }

        private Placeholder(string name, bool normalizeDescription = false)
        {
            this.Name = name;
            this.NormalizeDescription = normalizeDescription;
        }

        public override string ToString() => $"<{this.Name}>";
    }

    internal sealed class PatternNormalizer
    {
        private readonly string _pattern;
        private readonly IDictionary<string, string> _map;

        public PatternNormalizer(string pattern, SqlCodeAnalysisConfiguration configuration)
        {
            this._pattern = pattern;
            this._map = new Dictionary<string, string> { { Placeholder.Prefix.Name, configuration.NamingConventionPrefix } };
        }

        public void ResolvePlaceholder(string name, string value) => this._map.Add(name, value);

        public string Normalize()
        {
            string replacementPattern = String.Join("|", new[] { @"\*" }.Concat(this._map.Keys.Select(x => $@"\<{x}\>")));
            string mask = Regex.Replace(this._pattern, replacementPattern, this.OnMatch);
            return mask;
        }

        private string OnMatch(Match match) => this._map.TryGetValue(match.Value.TrimStart('<').TrimEnd('>'), out string replacement) ? replacement : match.Value;
    }
}