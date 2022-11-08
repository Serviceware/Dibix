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
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ILockEntryManager lockEntryManager
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            this._logger = logger;
            this._resolvers = new Collection<ActionTargetDefinitionResolver>
            {
                new ExternalReflectionActionTargetDefinitionResolver(schemaDefinitionResolver, schemaRegistry, lockEntryManager, logger)
              , new SqlStatementDefinitionActionTargetDefinitionResolver
                (
                    productName
                  , areaName
                  , className
                  , sqlStatementDefinitionProvider
                  , externalSchemaResolver
                  , referencedAssemblyInspector
                  , schemaDefinitionResolver
                  , schemaRegistry
                  , logger
                )
            //, new NeighborReflectionActionTargetDefinitionResolver(projectName, referencedAssemblyInspector, schemaRegistry, logger)
            };
        }

        public T Resolve<T>(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters) where T : ActionTargetDefinition, new()
        {
            foreach (ActionTargetDefinitionResolver resolver in this._resolvers)
            {
                if (resolver.TryResolve(targetName, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out T definition))
                    return definition;
            }

            this._logger.LogError($"Could not resolve action target: {targetName}", filePath, line, column);
            return null;
        }
    }
}