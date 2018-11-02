using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    internal sealed class OutputSelectResult
    {
        public int Index { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public IList<OutputColumnResult> Columns { get; private set; }

        public OutputSelectResult(int index, int line, int column)
        {
            this.Index = index;
            this.Line = line;
            this.Column = column;
            this.Columns = new Collection<OutputColumnResult>();
        }
    }
}