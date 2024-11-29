using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionTargetDefinitionResolverFacade
    {
        T Resolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody, Func<ActionTarget, T> actionTargetDefinitionFactory) where T : ActionTargetDefinition;
    }
}