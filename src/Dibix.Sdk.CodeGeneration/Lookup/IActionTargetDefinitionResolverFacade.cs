using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionTargetDefinitionResolverFacade
    {
        T Resolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters) where T : ActionTargetDefinition, new();
    }
}