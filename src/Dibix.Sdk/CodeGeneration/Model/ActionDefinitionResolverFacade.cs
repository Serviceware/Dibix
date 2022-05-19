using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.CodeGeneration.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionDefinitionResolverFacade : IActionDefinitionResolverFacade
    {
        private readonly ICollection<ActionDefinitionResolver> _resolvers;
        private readonly ILogger _logger;

        public ActionDefinitionResolverFacade
        (
            string productName
          , string areaName
          , string className
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , LockEntryManager lockEntryManager
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            this._logger = logger;
            this._resolvers = new Collection<ActionDefinitionResolver>
            {
                new ExternalReflectionTargetActionDefinitionResolver(schemaRegistry, lockEntryManager, logger)
              , new SqlStatementDefinitionActionDefinitionResolver
                (
                    productName
                  , areaName
                  , className
                  , sqlStatementDefinitionProvider
                  , externalSchemaResolver
                  , referencedAssemblyInspector
                  , schemaRegistry
                  , logger
                )
            //, new NeighborReflectionActionDefinitionResolver(projectName, referencedAssemblyInspector, schemaRegistry, logger)
            };
        }

        public ActionDefinition Resolve(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters)
        {
            foreach (ActionDefinitionResolver resolver in this._resolvers)
            {
                if (resolver.TryResolve(targetName, filePath, line, column, explicitParameters, pathParameters, bodyParameters, out ActionDefinition definition))
                    return definition;
            }

            this._logger.LogError($"Could not resolve action target: {targetName}", filePath, line, column);
            return null;
        }
    }
}