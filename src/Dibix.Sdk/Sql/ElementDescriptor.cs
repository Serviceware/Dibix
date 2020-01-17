using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal sealed class ElementDescriptor
    {
        public int Offset { get; }
        public TSqlObject Element { get; }

        public ElementDescriptor(int offset, TSqlObject element)
        {
            this.Offset = offset;
            this.Element = element;
        }
    }
}