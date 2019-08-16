using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("[{Kind}] {Name}")]
    public sealed class Constraint
    {
        public ConstraintKind Kind { get; }
        public string KindDisplayName { get; }
        public string Name { get; }
        public bool? IsClustered { get; }
        public SourceInformation Source { get; }
        public ConstraintDefinition Definition { get; }
        public IList<Column> Columns { get; }

        internal Constraint(ConstraintKind kind, string name, bool? isClustered, SourceInformation source, ConstraintDefinition definition)
        {
            this.Kind = kind;
            this.KindDisplayName = $"{String.Join(" ", kind.ToString().SplitWords())} constraint";
            this.Name = name;
            this.IsClustered = isClustered;
            this.Source = source;
            this.Definition = definition;
            this.Columns = new Collection<Column>();
        }
    }
}