using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IControllerDefinitionProvider
    {
        ICollection<ControllerDefinition> Controllers { get; }
        ICollection<SecurityScheme> SecuritySchemes { get; }
        bool HasSchemaErrors { get; }
    }
}