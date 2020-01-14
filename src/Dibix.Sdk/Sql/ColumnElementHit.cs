using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal sealed class ColumnElementHit
    {
        public int Offset { get; }
        public TSqlObject Element { get; }

        public ColumnElementHit(int offset, TSqlObject element)
        {
            this.Offset = offset;
            this.Element = element;
        }
    }
}