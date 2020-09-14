using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 17)]
    public sealed class NamingConventionSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            // Renaming this PK would force rebuilding the full text catalog which would be very slow
            ["PK_hlsysobjecdata"] = ""
            
            // Temporary helpLine common blob component workarounds
          , ["Blob"] = "361af17b783120bf3a57eb875ad5fcfd"
          , ["BlobDetail"] = "571cb352fdd9e58638c888cd8cfb6714"
          , ["fnSplit"] = "88d0aa3b3a42962c453a447f75ec497e"
          , ["spBlobSelect"] = "6e96ed9219fddff54c16ad757e32c0c5"
          , ["spBlobTextSearch"] = "d8fdc1ddf76edad129d6b79b4bb82601"

        };
        private static readonly IDictionary<string, string> ColumnSuppressions = new Dictionary<string, string>
        {
            ["hlsysbaselineattr#fixedvalue2"] = "655bc34ce38a042f2a76c3cd47bdc35d"
          , ["hlsysportalconfig#attrpathacc1"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysportalconfig#attrpathacc2"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysportalconfig#attrpathapp1"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysportalconfig#attrpathapp2"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysportalconfig#attrpathpassword1"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysportalconfig#attrpathpassword2"] = "db00f56842f2864e3ece85c61306ede3"
          , ["hlsysslmservicehoursentry#datetime1"] = "94a2e2bcc265aa23080876862d5ef58e"
          , ["hlsysslmservicehoursentry#datetime2"] = "94a2e2bcc265aa23080876862d5ef58e"
          , ["hlsysslmservicehoursentry#dayofweek1"] = "94a2e2bcc265aa23080876862d5ef58e"
          , ["hlsysslmservicehoursentry#dayofweek2"] = "94a2e2bcc265aa23080876862d5ef58e"
          , ["hlsysslmservicehoursentry#time1"] = "94a2e2bcc265aa23080876862d5ef58e"
          , ["hlsysslmservicehoursentry#time2"] = "94a2e2bcc265aa23080876862d5ef58e"
        };
        private readonly IDictionary<string, TSqlFragment> _looseConstraintDeclarations;

        protected override string ErrorMessageTemplate => "{0}";

        public NamingConventionSqlCodeAnalysisRule() => this._looseConstraintDeclarations = new Dictionary<string, TSqlFragment>();

        protected override void BeginStatement(TSqlScript node)
        {
            LooseConstraintDeclarationVisitor visitor = new LooseConstraintDeclarationVisitor();
            node.Accept(visitor);
            this._looseConstraintDeclarations.ReplaceWith(visitor.LooseConstraintDeclarations);
        }

        // Tables
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Check(node.SchemaObjectName, NamingConventions.Table, nameof(NamingConventions.Table));
            base.Visit(node);
        }

        // Views
        public override void Visit(CreateViewStatement node) => this.Check(node.SchemaObjectName, NamingConventions.View, nameof(NamingConventions.View));

        // UDTs
        public override void Visit(CreateTypeStatement node) => this.Check(node.Name, NamingConventions.Type, nameof(NamingConventions.Type));

        // Sequences
        public override void Visit(CreateSequenceStatement node) => this.Check(node.Name, NamingConventions.Sequence, nameof(NamingConventions.Sequence));

        // Stored procedures
        public override void Visit(CreateProcedureStatement node) => this.Check(node.ProcedureReference.Name, NamingConventions.Procedure, nameof(NamingConventions.Procedure));

        // Functions
        public override void Visit(CreateFunctionStatement node) => this.Check(node.Name, NamingConventions.Function, nameof(NamingConventions.Function));

        // Table columns
        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            // Columns
            foreach (ColumnDefinition column in tableDefinition.ColumnDefinitions)
            {
                if (Regex.IsMatch(column.ColumnIdentifier.Value, NamingConventions.Column.Pattern))
                    continue;

                if (ColumnSuppressions.TryGetValue($"{tableName.BaseIdentifier.Value}#{column.ColumnIdentifier.Value}", out string hash) && hash == base.Hash) 
                    continue;

                base.Fail(column, $"Column names should only contain the characters 'a-z_' and have no trailing underscores: {tableName.BaseIdentifier.Value}.{column.ColumnIdentifier.Value}");
            }

            this.VisitConstraints(tableModel, tableName, tableDefinition);
            this.VisitIndexes(tableModel, tableName, tableDefinition);
        }

        // Constraints
        private void VisitConstraints(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            foreach (Constraint constraint in base.Model.GetConstraints(tableModel, tableName))
            {
                if (constraint.Kind == ConstraintKind.Nullable)
                    return;

                if (constraint.Name == null)
                    return;

                NamingConvention namingConvention = GetNamingConvention(constraint.Kind);
                if (!this._looseConstraintDeclarations.TryGetValue(constraint.Name, out TSqlFragment target))
                {
                    TSqlFragment sqlFragment = tableDefinition.FindChild(constraint.Source);
                    target = ExtractConstraintIdentifier(sqlFragment, constraint.Name);
                }

                this.Check(constraint.Name, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, constraint.Columns), target, constraint.KindDisplayName);
            }
        }

        private static Identifier ExtractConstraintIdentifier(TSqlFragment fragment, string name) => GetConstraintDefinition(fragment, name).ConstraintIdentifier;

        private static ConstraintDefinition GetConstraintDefinition(TSqlFragment fragment, string name)
        {
            switch (fragment)
            {
                case AlterTableAddTableElementStatement alterTableAddTableElementStatement:
                    return alterTableAddTableElementStatement.Definition
                                                             .TableConstraints
                                                             .Single(x => x.ConstraintIdentifier.Value == name);

                case ConstraintDefinition constraintDefinition:
                    return constraintDefinition;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fragment), fragment, null);
            }
        }

        // Indexes
        private void VisitIndexes(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
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

                if (!this._looseConstraintDeclarations.TryGetValue(index.Name, out TSqlFragment target))
                {
                    TSqlFragment sqlFragment = tableDefinition.FindChild(index.Source);
                    target = ExtractIndexIdentifier(sqlFragment);
                }

                this.Check(index.Name, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, index.Columns), target, displayName);
            }
        }

        private static Identifier ExtractIndexIdentifier(TSqlFragment fragment)
        {
            switch (fragment)
            {
                case CreateIndexStatement createIndexStatement: return createIndexStatement.Name;
                case IndexStatement indexStatement: return indexStatement.Name;
                case IndexDefinition indexDefinition: return indexDefinition.Name;
                default: throw new ArgumentOutOfRangeException(nameof(fragment), fragment, null);
            }
        }

        private void Check(SchemaObjectName schemaObjectName, NamingConvention namingConvention, string displayName) => this.Check(schemaObjectName.BaseIdentifier.Value, namingConvention, null, base.Fail, schemaObjectName.BaseIdentifier, displayName);
        private void Check(string name, NamingConvention namingConvention, Action<PatternNormalizer> replacements, TSqlFragment target, string displayName) => this.Check(name, namingConvention, replacements, base.Fail, target, displayName);
        private void Check<T>(string name, NamingConvention namingConvention, Action<PatternNormalizer> replacements, Action<T, object[]> failAction, T target, string displayName)
        {
            string mask = namingConvention.NormalizePattern(base.Configuration, replacements);
            string description = namingConvention.NormalizeDescription(this.Configuration);
            if (Regex.IsMatch(name, mask))
                return;

            if (Suppressions.TryGetValue(name, out string hash) && hash == base.Hash) 
                return;

            failAction(target, new object[] { $"{displayName} '{name}' does not match naming convention '{description}'. Also make sure the name is all lowercase." });
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

        private sealed class LooseConstraintDeclarationVisitor : TSqlFragmentVisitor
        {
            public IDictionary<string, TSqlFragment> LooseConstraintDeclarations { get; }

            public LooseConstraintDeclarationVisitor() => this.LooseConstraintDeclarations = new Dictionary<string, TSqlFragment>();

            public override void Visit(CreateIndexStatement node) => this.LooseConstraintDeclarations.Add(node.Name.Value, node.Name);
            
            public override void Visit(AlterTableAddTableElementStatement node)
            {
                node.Definition
                    .TableConstraints
                    .Each(x => this.LooseConstraintDeclarations.Add(x.ConstraintIdentifier.Value, x.ConstraintIdentifier));
            }
        }
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