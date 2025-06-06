﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 17)]
    public sealed class NamingConventionSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private readonly IDictionary<string, TSqlFragment> _looseConstraintDeclarations;

        protected override string ErrorMessageTemplate => "{0}";

        public NamingConventionSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context)
        {
            _looseConstraintDeclarations = new Dictionary<string, TSqlFragment>();
        }

        protected override void BeginStatement(TSqlScript node)
        {
            LooseConstraintDeclarationVisitor visitor = new LooseConstraintDeclarationVisitor();
            node.Accept(visitor);
            _looseConstraintDeclarations.ReplaceWith(visitor.LooseConstraintDeclarations);
        }

        public override void Visit(BeginTransactionStatement node)
        {
            if (node.Name == null)
                return;

            string name = node.Name.Value;
            if (name.Length > 32)
            {
                Fail(node.Name, $"The identifier that starts with '{name}' is too long. Maximum length is 32.");
            }
        }

        // Tables
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            Check(node.SchemaObjectName, NamingConventions.Table);
            base.Visit(node);
        }

        // Views
        public override void Visit(CreateViewStatement node) => Check(node.SchemaObjectName, NamingConventions.View);

        // UDTs
        public override void Visit(CreateTypeStatement node) => Check(node.Name, NamingConventions.Type);

        // Sequences
        public override void Visit(CreateSequenceStatement node) => Check(node.Name, NamingConventions.Sequence);

        // Stored procedures
        public override void Visit(CreateProcedureStatement node) => Check(node.ProcedureReference.Name, NamingConventions.Procedure);

        // Functions
        public override void Visit(CreateFunctionStatement node) => Check(node.Name, NamingConventions.Function);

        // Function parameters
        public override void Visit(ProcedureParameter node) => Check(node.VariableName, NamingConventions.FunctionParameter);

        // Table columns
        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            // Columns
            foreach (ColumnDefinition column in tableDefinition.ColumnDefinitions)
            {
                if (Regex.IsMatch(column.ColumnIdentifier.Value, NamingConventions.Column.Pattern))
                    continue;

                string suppressionKey = $"{tableName.BaseIdentifier.Value}#{column.ColumnIdentifier.Value}";
                FailIfUnsuppressed(column, suppressionKey, $"Column names should only contain the characters 'a-z_' and have no trailing underscores: {tableName.BaseIdentifier.Value}.{column.ColumnIdentifier.Value}");
            }

            VisitConstraints(tableModel, tableName, tableDefinition);
            VisitIndexes(tableModel, tableName, tableDefinition);
        }

        // Constraints
        private void VisitConstraints(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            foreach (Constraint constraint in Model.GetConstraints(tableModel, tableName).OrderBy(x => x.Source.StartLine))
            {
                if (constraint.Kind == ConstraintKind.Nullable)
                    return;

                if (constraint.Name == null)
                    return;

                NamingConvention namingConvention = GetNamingConvention(constraint.Kind);
                if (!_looseConstraintDeclarations.TryGetValue(constraint.Name, out TSqlFragment target))
                {
                    TSqlFragment sqlFragment = tableDefinition.FindChild(constraint.Source);
                    target = ExtractConstraintIdentifier(sqlFragment, constraint.Name);
                }

                Check(constraint.Name, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, constraint.Columns), target);
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
            foreach (Sql.Index index in Model.GetIndexes(tableModel, tableName).OrderBy(x => x.Source.StartLine))
            {
                NamingConvention namingConvention = index.IsUnique ? NamingConventions.UniqueIndex : NamingConventions.Index;
                if (!_looseConstraintDeclarations.TryGetValue(index.Name, out TSqlFragment target))
                {
                    TSqlFragment sqlFragment = tableDefinition.FindChild(index.Source);
                    target = ExtractIndexIdentifier(sqlFragment);
                }

                Check(index.Name, namingConvention, x => ResolveConstraintPlaceholders(x, tableName.BaseIdentifier.Value, index.Columns), target);
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

        private void Check(SchemaObjectName schemaObjectName, NamingConvention namingConvention) => Check(schemaObjectName.BaseIdentifier, namingConvention);
        private void Check(Identifier identifier, NamingConvention namingConvention) => Check(identifier.Value, namingConvention, null, FailIfUnsuppressed, identifier);
        private void Check(string name, NamingConvention namingConvention, Action<PatternNormalizer> replacements, TSqlFragment target) => Check(name, namingConvention, replacements, FailIfUnsuppressed, target);
        private void Check<T>(string name, NamingConvention namingConvention, Action<PatternNormalizer> replacements, Action<T, string, object[]> failAction, T target)
        {
            string namingConventionPrefix = Configuration.NamingConventionPrefix;
            string mask = namingConvention.NormalizePattern(namingConventionPrefix, replacements);
            string description = namingConvention.NormalizeDescription(namingConventionPrefix);
            if (Regex.IsMatch(name, mask))
                return;

            failAction(target, name, new object[] { $"{namingConvention.DisplayName} '{name}' does not match naming convention '{description}'. Also make sure the name is all lowercase." });
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

            public LooseConstraintDeclarationVisitor() => LooseConstraintDeclarations = new Dictionary<string, TSqlFragment>();

            public override void Visit(CreateIndexStatement node) => LooseConstraintDeclarations.Add(node.Name.Value, node.Name);

            public override void Visit(AlterTableAddTableElementStatement node)
            {
                node.Definition
                    .TableConstraints
                    .Each(x => LooseConstraintDeclarations.Add(x.ConstraintIdentifier.Value, x.ConstraintIdentifier));
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
        public static readonly NamingConvention FunctionParameter    = new NamingConvention($"^@{TextRegex}$");
        public static readonly NamingConvention Column               = new NamingConvention("^[a-z](([a-z_]+)?[a-z])?$");
    }

    internal readonly struct NamingConvention
    {
        public string DisplayName { get; }
        public string Pattern { get; }
        public string Documentation { get; }

        public NamingConvention(string pattern, string documentation = null, [CallerMemberName] string name = null)
        {
            Pattern = pattern;
            Documentation = documentation ?? pattern;
            DisplayName = ComputeDisplayName(name);
        }

        public string NormalizePattern(string namingConventionPrefix, Action<PatternNormalizer> replacements)
        {
            PatternNormalizer normalizer = new PatternNormalizer(Pattern, namingConventionPrefix);
            replacements?.Invoke(normalizer);
            return normalizer.Normalize();
        }

        public string NormalizeDescription(string namingConventionPrefix)
        {
            PatternNormalizer normalizer = new PatternNormalizer(Documentation, namingConventionPrefix);
            return normalizer.Normalize();
        }

        private static string ComputeDisplayName(string name)
        {
            string displayName = Regex.Replace(name, @"(\B[A-Z])", x => $" {x.Value.ToLowerInvariant()}");
            return displayName;
        }
    }

    internal readonly struct Placeholder
    {
        public static readonly Placeholder Prefix = new Placeholder("prefix", true);
        public static readonly Placeholder Table  = new Placeholder("table");
        public static readonly Placeholder Column = new Placeholder("column");

        public string Name { get; }
        public bool NormalizeDescription { get; }

        private Placeholder(string name, bool normalizeDescription = false)
        {
            Name = name;
            NormalizeDescription = normalizeDescription;
        }

        public override string ToString() => $"<{Name}>";
    }

    internal sealed class PatternNormalizer
    {
        private readonly string _pattern;
        private readonly IDictionary<string, string> _map;

        public PatternNormalizer(string pattern, string namingConventionPrefix)
        {
            _pattern = pattern;
            _map = new Dictionary<string, string> { { Placeholder.Prefix.Name, namingConventionPrefix } };
        }

        public void ResolvePlaceholder(string name, string value) => _map.Add(name, value);

        public string Normalize()
        {
            string replacementPattern = String.Join("|", EnumerableExtensions.Create(@"\*").Concat(_map.Keys.Select(x => $@"\<{x}\>")));
            string mask = Regex.Replace(_pattern, replacementPattern, OnMatch);
            return mask;
        }

        private string OnMatch(Match match) => _map.TryGetValue(match.Value.TrimStart('<').TrimEnd('>'), out string replacement) ? replacement : match.Value;
    }
}