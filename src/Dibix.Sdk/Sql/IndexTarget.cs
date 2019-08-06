using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{Name.BaseIdentifier.Value}")]
    public sealed class IndexTarget
    {
        public SchemaObjectName Name { get; }
        public ICollection<Index> Indexes { get; }

        internal IndexTarget(SchemaObjectName name)
        {
            this.Name = name;
            this.Indexes = new Collection<Index>();
        }
    }
}