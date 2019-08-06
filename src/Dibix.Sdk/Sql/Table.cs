using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{Type} - {Name.BaseIdentifier.Value}")]
    public sealed class Table
    {
        public TableType Type { get; }
        public SchemaObjectName Name { get; }
        public TableDefinition Definition { get; }

        internal Table(TableType type, SchemaObjectName name, TableDefinition definition)
        {
            this.Type = type;
            this.Name = name;
            this.Definition = definition;
        }
    }
}