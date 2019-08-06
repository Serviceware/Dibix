using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{(IsUnique ? \"Unique\" : \"Nonunique\")} - {Identifier.Value}")]
    public sealed class Index
    {
        public TSqlStatement Target { get; }
        public Identifier Identifier { get; }
        public bool IsUnique { get; }
        public IList<ColumnReference> Columns { get; }

        internal Index(TSqlStatement target, Identifier identifier, bool isUnique, IEnumerable<ColumnReference> columns)
        {
            this.Target = target;
            this.Identifier = identifier;
            this.IsUnique = isUnique;
            this.Columns = new Collection<ColumnReference>();
            this.Columns.AddRange(columns);
        }
    }
}