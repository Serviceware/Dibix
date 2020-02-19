using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IUserDefinedTypeProvider
    {
        ICollection<UserDefinedTypeDefinition> Types { get; }
    }
}