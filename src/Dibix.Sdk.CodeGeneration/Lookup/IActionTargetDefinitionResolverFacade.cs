using System.Collections.Generic;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionTargetDefinitionResolverFacade
    {
        T Resolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody, IDictionary<HttpStatusCode,ActionResponse> responses) where T : ActionTargetDefinition, new();
    }
}