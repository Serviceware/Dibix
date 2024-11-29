using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a SQL statement within the current project or an external project
    internal sealed class SqlStatementDefinitionActionTargetDefinitionResolver : ActionTargetDefinitionResolver
    {
        #region Fields
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _className;
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Constructor
        public SqlStatementDefinitionActionTargetDefinitionResolver
        (
            string productName
          , string areaName
          , string className
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        ) : base(schemaRegistry, logger)
        {
            _productName = productName;
            _areaName = areaName;
            _className = className;
            _schemaRegistry = schemaRegistry;
        }
        #endregion

        #region Overrides
        public override bool TryResolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, ActionRequestBody requestBody, Func<ActionTarget, T> actionTargetDefinitionFactory, out T actionTargetDefinition)
        {
            if (!TryGetStatementDefinitionByProbing(targetName, out SqlStatementDefinition statementDefinition))
            {
                actionTargetDefinition = null;
                return false;
            }

            string accessorClassName = statementDefinition.ExternalSchemaInfo?.Owner.DefaultClassName ?? _className;
            string localAccessorFullName = $"{statementDefinition.AbsoluteNamespace}.{_className}";
            string externalAccessorFullName = $"{statementDefinition.AbsoluteNamespace}.{accessorClassName}";
            string definitionName = statementDefinition.DefinitionName;
            bool isAsync = statementDefinition.Async;
            bool hasRefParameters = statementDefinition.Parameters.Any(x => x.IsOutput);
            ActionTarget actionTarget = new LocalActionTarget(statementDefinition, localAccessorFullName, externalAccessorFullName, definitionName, isAsync, hasRefParameters, sourceLocation);
            actionTargetDefinition = CreateActionTargetDefinition(actionTarget, pathParameters, requestBody, actionTargetDefinitionFactory);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionTargetDefinition, pathParameters);
            foreach (SqlQueryParameter parameter in statementDefinition.Parameters)
            {
                base.CollectActionParameter
                (
                    parameter.Name
                  , parameter.Type
                  , parameter.DefaultValue
                  , parameter.IsOutput
                  , targetName
                  , sourceLocation
                  , parameterRegistry
                  , explicitParameters
                  , pathParameters
                  , bodyParameters
                );
            }

            foreach (ExplicitParameter explicitParameter in explicitParameters.Values.Where(x => !x.Visited).ToArray())
            {
                if (ValidateImplicitParameterMetadata(explicitParameter))
                    continue;

                explicitParameter.Visited = true;
            }

            if (statementDefinition.Results.Any(x => x.ResultMode == SqlQueryResultMode.Single) && actionTargetDefinition.Responses.ContainsKey(HttpStatusCode.NotFound))
            {
                // Automatic status code detection
                actionTargetDefinition.Responses.Add(HttpStatusCode.NotFound, new ActionResponse(HttpStatusCode.NotFound));
            }

            if (actionTargetDefinition is ActionDefinition actionDefinition)
                CollectResponse(actionDefinition, statementDefinition);

            foreach (ErrorResponse errorResponse in statementDefinition.ErrorResponses)
            {
                actionTargetDefinition.RegisterErrorResponse(errorResponse.StatusCode, errorResponse.ErrorCode, errorResponse.ErrorDescription, errorResponse.SourceLocation, Logger);
            }

            return true;
        }
        #endregion

        #region Private Methods
        private bool TryGetStatementDefinitionByProbing(string targetName, out SqlStatementDefinition statementDefinition)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(_productName, _areaName, LayerName.Data, relativeNamespace: null, targetName))
            {
                if (_schemaRegistry.TryGetSchema(candidate, out statementDefinition))
                    return true;
            }
            statementDefinition = null;
            return false;
        }

        private static void CollectResponse(ActionDefinition actionDefinition, SqlStatementDefinition definition)
        {
            if (definition.FileResult != null)
                actionDefinition.SetFileResponse(new ActionFileResponse(HttpMediaType.Binary), definition.FileResult.Location);
            else
                actionDefinition.DefaultResponseType = definition.ResultType;
        }
        #endregion
    }
}