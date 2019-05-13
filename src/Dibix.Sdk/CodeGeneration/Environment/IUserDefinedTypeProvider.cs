using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IUserDefinedTypeProvider
    {
        IEnumerable<UserDefinedTypeDefinition> Types { get; }
    }
}