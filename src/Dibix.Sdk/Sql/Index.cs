using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{(IsUnique ? \"Unique\" : \"Nonunique\")} - {Name}")]
    public sealed class Index
    {
        public string Name { get; }
        public bool IsUnique { get; }
        public bool IsClustered { get; }
        public SourceInformation Source { get; }
        public Identifier Identifier { get; }
        public TSqlFragment Definition { get; }
        public IList<Column> Columns { get; }

        internal Index(string name, bool isUnique, bool isClustered, SourceInformation source, Identifier identifier, TSqlFragment definition)
        {
            this.Name = name;
            this.IsUnique = isUnique;
            this.IsClustered = isClustered;
            this.Source = source;
            this.Identifier = identifier;
            this.Definition = definition;
            this.Columns = new Collection<Column>();
        }
    }
}