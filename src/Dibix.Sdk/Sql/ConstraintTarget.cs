using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{Name.BaseIdentifier.Value}")]
    public sealed class ConstraintTarget
    {
        public SchemaObjectName Name { get; }
        public ICollection<Constraint> Constraints { get; }

        internal ConstraintTarget(SchemaObjectName name)
        {
            this.Name = name;
            this.Constraints = new Collection<Constraint>();
        }
    }
}