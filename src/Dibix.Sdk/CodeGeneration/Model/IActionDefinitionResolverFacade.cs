using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionDefinitionResolverFacade
    {
        ActionDefinition Resolve(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters);
    }
}