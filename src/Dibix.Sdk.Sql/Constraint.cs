using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.Dac;

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
        public string CheckCondition { get; }
        public IList<Column> Columns { get; }

        internal Constraint(ConstraintKind kind, string name, bool? isClustered, SourceInformation source, string checkCondition)
        {
            this.Kind = kind;
            this.KindDisplayName = $"{String.Join(" ", kind.ToString().SplitWords())} constraint";
            this.Name = name;
            this.IsClustered = isClustered;
            this.Source = source;
            this.CheckCondition = checkCondition;
            this.Columns = new Collection<Column>();
        }
    }
}