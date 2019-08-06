using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{Type} - {Definition.Dump()}")]
    public sealed class Constraint
    {
        public ConstraintDefinition Definition { get; }
        public ConstraintType Type { get; }
        public string TypeDisplayName { get; }
        public IList<ColumnReference> Columns { get; }

        internal Constraint(ConstraintDefinition definition, ConstraintType type, IEnumerable<ColumnReference> columns)
        {
            this.Definition = definition;
            this.Type = type;
            this.TypeDisplayName = $"{String.Join(" ", type.ToString().SplitWords())} constraint";
            this.Columns = new Collection<ColumnReference>();
            this.Columns.AddRange(columns);
        }
    }
}