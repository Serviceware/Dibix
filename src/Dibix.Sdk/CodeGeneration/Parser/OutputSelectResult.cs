using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OutputSelectResult
    {
        public int Index { get; }
        public int Line { get; }
        public int Column { get; }
        public IList<OutputColumnResult> Columns { get; }

        public OutputSelectResult(int index, int line, int column)
        {
            this.Index = index;
            this.Line = line;
            this.Column = column;
            this.Columns = new Collection<OutputColumnResult>();
        }
    }
}