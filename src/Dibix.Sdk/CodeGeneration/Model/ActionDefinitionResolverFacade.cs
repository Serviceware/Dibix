using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionDefinitionResolverFacade : IActionDefinitionResolverFacade
    {
        private readonly ICollection<ActionDefinitionResolver> _resolvers;
        private readonly ILogger _logger;

        public ActionDefinitionResolverFacade
        (
            string projectName
          , string rootNamespace
          , string productName
          , string areaName
          , string className
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , LockEntryManager lockEntryManager
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            this._logger = logger;
            this._resolvers = new Collection<ActionDefinitionResolver>
            {
                new ExternalActionDefinitionResolver(schemaRegistry, lockEntryManager, logger)
              , new LocalActionDefinitionResolver
                (
                    rootNamespace
                  , productName
                  , areaName
                  , className
                  , sqlStatementDefinitionProvider
                  , schemaRegistry
                  , logger
                )
              , new NeighborActionDefinitionResolver(projectName, referencedAssemblyInspector, schemaRegistry, logger)
            };
        }

        public ActionDefinition Resolve(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters)
        {
            foreach (ActionDefinitionResolver resolver in this._resolvers)
            {
                if (resolver.TryResolve(targetName, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out ActionDefinition definition))
                    return definition;
            }

            this._logger.LogError(null, $"Could not resolve action target: {targetName}", filePath, line, column);
            return null;
        }
    }
}