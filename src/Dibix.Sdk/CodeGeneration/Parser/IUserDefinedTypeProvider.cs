using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IUserDefinedTypeProvider : ISchemaProvider
    {
        IEnumerable<UserDefinedTypeSchema> Types { get; }
    }
}