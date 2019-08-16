using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class RedundantIndexSqlCodeAnalysisRule : SqlCodeAnalysisRule<RedundantIndexSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 33;
        public override string ErrorMessage => "{0}";
    }

    public sealed class RedundantIndexSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            IEnumerable<IndexHit> constraints = base.Model
                                                    .GetConstraints(tableModel, tableName)
                                                    .Where(x => x.Kind == ConstraintKind.PrimaryKey || x.Kind == ConstraintKind.Unique)
                                                    .Select(x => new IndexHit(x.Definition.ConstraintIdentifier, x.Definition, x.Columns));

            IEnumerable<IndexHit> indexes = base.Model
                                                .GetIndexes(tableModel, tableName)
                                                .Select(index => new IndexHit(index.Identifier, index.Definition, index.Columns));

            IEnumerable<IGrouping<IndexHit, IndexHit>> matches = constraints.Concat(indexes)
                                                                            .GroupBy(x => x, new IndexHitComparer())
                                                                            .Where(x => x.Count() > 1);
            foreach (IGrouping<IndexHit, IndexHit> match in matches)
            {
                bool differentIncludeColumns = match.Select(x => x.IncludeColumns).Distinct().Count() > 1;
                base.Fail(match.First().Definition, $"Found duplicate indexes{(differentIncludeColumns ? " with different includes" : null)}: {String.Join(", ", match.Select(x => x.Identifier.Value))}");
            }
        }

        private class IndexHit
        {
            public Identifier Identifier { get; }
            public TSqlFragment Definition { get; }
            public string Columns { get; }
            public string Filter { get; private set; }
            public string IncludeColumns { get; private set; }

            public IndexHit(Identifier identifier, TSqlFragment definition, IEnumerable<Column> columns)
            {
                this.Identifier = identifier;
                this.Definition = definition;
                this.Columns = String.Join(",", columns.Select(x => x.Name));
                this.ApplyAdditionalIndexFeatures(definition);
            }

            private void ApplyAdditionalIndexFeatures(TSqlFragment definition)
            {
                BooleanExpression filter = null;
                switch (definition)
                {
                    case IndexDefinition indexDefinition:
                        filter = indexDefinition.FilterPredicate;
                        break;

                    case CreateIndexStatement indexStatement:
                        filter = indexStatement.FilterPredicate;
                        this.IncludeColumns = String.Join(",", indexStatement.IncludeColumns.Select(x => x.MultiPartIdentifier.Identifiers.Last().Value));
                        break;
                }

                if (filter != null)
                    this.Filter = NormalizeFilterCondition(filter);
            }

            private static string NormalizeFilterCondition(BooleanExpression filter) => ScriptDomFacade.Generate(filter);
        }

        private sealed class IndexHitComparer : IEqualityComparer<IndexHit>
        {
            bool IEqualityComparer<IndexHit>.Equals(IndexHit x, IndexHit y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (ReferenceEquals(x, null))
                    return false;

                if (ReferenceEquals(y, null))
                    return false;

                if (x.GetType() != y.GetType())
                    return false;

                return String.Equals(x.Columns, y.Columns)
                    && String.Equals(x.Filter, y.Filter);
            }

            int IEqualityComparer<IndexHit>.GetHashCode(IndexHit obj)
            {
                unchecked
                {
                    int hashCode = obj.Columns != null ? obj.Columns.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (obj.Filter != null ? obj.Filter.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
