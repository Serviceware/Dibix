using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public class TableDefinitionVisitor : TSqlFragmentVisitor
    {
        public ICollection<Table> Tables { get; }

        public TableDefinitionVisitor()
        {
            this.Tables = new Collection<Table>();
        }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Visit(TableType.Table, node.SchemaObjectName, node.Definition);
        }

        public override void Visit(CreateTypeTableStatement node) => this.Visit(TableType.TypeTable, node.Name, node.Definition);

        protected virtual void Visit(Table table) { }
        
        private void Visit(TableType type, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            Table table = new Table(type, tableName, tableDefinition);
            this.Tables.Add(table);
            this.Visit(table);
        }
    }
}