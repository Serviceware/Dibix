using System.Collections.Generic;

namespace Dibix.Sdk
{
    public interface ISourceSelection
    {
        ICollection<string> Files { get; }
        ISqlStatementParser Parser { get; }
    }
}