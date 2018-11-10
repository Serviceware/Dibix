using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISourceSelection
    {
        ICollection<string> Files { get; }
        ISqlStatementParser Parser { get; }
    }
}