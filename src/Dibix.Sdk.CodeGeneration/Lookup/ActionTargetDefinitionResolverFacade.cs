using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionTargetDefinitionResolverFacade : IActionTargetDefinitionResolverFacade
    {
        private readonly ICollection<ActionTargetDefinitionResolver> _resolvers;
        private readonly ILogger _logger;

        public ActionTargetDefinitionResolverFacade
        (
            string productName
          , string areaName
          , string className
          , ILockEntryManager lockEntryManager
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            this._logger = logger;
            this._resolvers = new Collection<ActionTargetDefinitionResolver>
            {
                new ExternalReflectionActionTargetDefinitionResolver(schemaRegistry, lockEntryManager, logger)
              , new SqlStatementDefinitionActionTargetDefinitionResolver
                (
                    productName
                  , areaName
                  , className
                  , schemaRegistry
                  , logger
                )
            };
        }

        public T Resolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody) where T : ActionTargetDefinition, new()
        {
            foreach (ActionTargetDefinitionResolver resolver in this._resolvers)
            {
                if (resolver.TryResolve(targetName, sourceLocation, explicitParameters, pathParameters, bodyParameters, requestBody, out T definition))
                    return definition;
            }

            this._logger.LogError($"Could not resolve action target: {targetName}", sourceLocation.Source, sourceLocation.Line, sourceLocation.Column);
            return null;
        }
    }
}