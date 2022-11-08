using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.Dac;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{(IsUnique ? \"Unique\" : \"Nonunique\")} - {Name}")]
    public sealed class Index
    {
        public string Name { get; }
        public bool IsUnique { get; }
        public bool IsClustered { get; }
        public SourceInformation Source { get; }
        public IList<Column> Columns { get; }
        public string Filter { get; }
        public ICollection<string> IncludeColumns { get; }

        internal Index(string name, bool isUnique, bool isClustered, SourceInformation source, string filter)
        {
            this.Name = name;
            this.IsUnique = isUnique;
            this.IsClustered = isClustered;
            this.Source = source;
            this.Columns = new Collection<Column>();
            this.Filter = filter;
            this.IncludeColumns = new Collection<string>();
        }
    }
}