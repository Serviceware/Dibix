using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal sealed class ColumnReference
    {
        public string Name { get; }
        public TSqlFragment Hit { get; }

        public ColumnReference(string name, TSqlFragment hit)
        {
            this.Name = name;
            this.Hit = hit;
        }
    }
}