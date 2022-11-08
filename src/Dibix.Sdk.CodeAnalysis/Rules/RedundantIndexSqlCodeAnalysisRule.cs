using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 33)]
    public sealed class RedundantIndexSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0}";

        public RedundantIndexSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            IEnumerable<IndexHit> constraints = base.Model
                                                    .GetConstraints(tableModel, tableName)
                                                    .Where(x => x.Kind == ConstraintKind.PrimaryKey || x.Kind == ConstraintKind.Unique)
                                                    .Select(x => new IndexHit(x));

            IEnumerable<IndexHit> indexes = base.Model
                                                .GetIndexes(tableModel, tableName)
                                                .Select(x => new IndexHit(x));

            IEnumerable<IGrouping<IndexHit, IndexHit>> matches = constraints.Concat(indexes)
                                                                            .GroupBy(x => x, new IndexHitComparer())
                                                                            .Where(x => x.Count() > 1);
            foreach (IGrouping<IndexHit, IndexHit> match in matches)
            {
                bool differentIncludeColumns = match.Select(x => x.IncludeColumns).Distinct().Count() > 1;
                base.Fail(match.First().Source, $"Found duplicate indexes{(differentIncludeColumns ? " with different includes" : null)}: {String.Join(", ", match.Select(x => x.Name))}");
            }
        }

        private class IndexHit
        {
            public SourceInformation Source { get; }
            public string Name { get; }
            public string Columns { get; }
            public string Filter { get; }
            public string IncludeColumns { get; }

            public IndexHit(Constraint constraint)
            {
                this.Source = constraint.Source;
                this.Name = constraint.Name;
                this.Columns = String.Join(",", constraint.Columns.Select(x => x.Name));
            }
            public IndexHit(Index index)
            {
                this.Source = index.Source;
                this.Name = index.Name;
                this.Columns = String.Join(",", index.Columns.Select(x => x.Name));
                this.IncludeColumns = String.Join(",", index.IncludeColumns);
                if (!this.IncludeColumns.Any())
                    this.IncludeColumns = null;

                this.Filter = index.Filter.NormalizeBooleanExpression();
            }
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
