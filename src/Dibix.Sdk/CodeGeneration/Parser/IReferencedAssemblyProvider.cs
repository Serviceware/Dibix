using System.Collections.Generic;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IReferencedAssemblyProvider
    {
        IEnumerable<Assembly> ReferencedAssemblies { get; }
    }
}