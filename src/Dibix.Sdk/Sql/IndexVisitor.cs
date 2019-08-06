using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public class IndexVisitor : TSqlFragmentVisitor
    {
        public IDictionary<string, IndexTarget> Targets { get; }

        public IndexVisitor()
        {
            this.Targets = new Dictionary<string, IndexTarget>();
        }

        public override void Visit(CreateTableStatement node) => this.Visit(node.SchemaObjectName, node.Definition, CollectIndexes);

        public override void Visit(CreateTypeTableStatement node) => this.Visit(node.Name, node.Definition, CollectIndexes);

        public override void Visit(CreateIndexStatement node) => this.Visit(node.OnName, node, CollectIndexes);

        private void Visit<TStatement>(SchemaObjectName name, TStatement statement, Func<TStatement, IEnumerable<Index>> indexSelector) where TStatement : TSqlFragment
        {
            string key = name.ToKey();
            if (!this.Targets.TryGetValue(key, out IndexTarget table))
            {
                table = new IndexTarget(name);
                this.Targets.Add(key, table);
            }
            table.Indexes.AddRange(indexSelector(statement));
        }

        private static IEnumerable<Index> CollectIndexes(TableDefinition table)
        {
            foreach (ColumnDefinition column in table.ColumnDefinitions.Where(x => x.Index != null))
                yield return Create(column.Index, column.Index.Name, column.Index.Unique, column.Index.Columns);

            foreach (IndexDefinition index in table.Indexes)
                yield return Create(index, index.Name, index.Unique, index.Columns);
        }

        private static IEnumerable<Index> CollectIndexes(CreateIndexStatement index)
        {
            yield return Create(index, index.Name, index.Unique, index.Columns);
        }

        private static Index Create(TSqlStatement target, Identifier identifier, bool isUnique, IEnumerable<ColumnWithSortOrder> columns)
        {
            IEnumerable<ColumnReference> columnReferences = columns.Select(x => new ColumnReference(x.Column.MultiPartIdentifier.Identifiers.Last().Value, x));
            return new Index(target, identifier, isUnique, columnReferences);
        }
    }
}