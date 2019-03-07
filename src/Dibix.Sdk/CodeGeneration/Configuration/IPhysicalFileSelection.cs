using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IPhysicalFileSelection
    {
        IEnumerable<string> Files { get; }
    }
}