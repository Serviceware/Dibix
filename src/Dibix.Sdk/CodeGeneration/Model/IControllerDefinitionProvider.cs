using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IControllerDefinitionProvider
    {
        ICollection<ControllerDefinition> Controllers { get; }
        bool HasSchemaErrors { get; }
    }
}