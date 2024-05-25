using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionTargetDefinitionResolverFacade
    {
        T Resolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody) where T : ActionTargetDefinition, new();
    }
}