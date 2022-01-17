using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a SQL statement within the current project
    internal sealed class LocalActionDefinitionResolver : ActionDefinitionResolver
    {
        #region Fields
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _className;
        private readonly ISqlStatementDefinitionProvider _sqlStatementDefinitionProvider;
        #endregion

        #region Constructor
        public LocalActionDefinitionResolver
        (
            string rootNamespace
          , string productName
          , string areaName
          , string className
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        ) : base(schemaRegistry, logger)
        {
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._className = className;
            this._sqlStatementDefinitionProvider = sqlStatementDefinitionProvider;
        }
        #endregion

        #region Overrides
        public override bool TryResolve(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out ActionDefinition actionDefinition)
        {
            // Use explicit namespace if it can be extracted
            int statementNameIndex = targetName.LastIndexOf('.');
            string @namespace = statementNameIndex >= 0 ? targetName.Substring(0, statementNameIndex) : null;

            // Detect absolute namespace if it is prefixed with the product name
            // i.E.: Data.Runtime is a relative namespace
            bool isAbsolute = targetName.StartsWith($"{this._productName}.", StringComparison.Ordinal);
            string normalizedNamespace = @namespace;
            if (!isAbsolute)
                normalizedNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.Data, @namespace);

            string methodName = targetName.Substring(statementNameIndex + 1);

            if (!this._sqlStatementDefinitionProvider.TryGetDefinition(normalizedNamespace, methodName, out SqlStatementDefinition definition))
            {
                actionDefinition = null;
                return false;
            }
            
            // Relative namespaces can not be resolved in neighbor projects
            /*
            if (!isAbsolute)
            {
                base.Logger.LogError(null, $@"Could not find action target: {target}
Tried: {normalizedNamespace}.{methodName}", filePath, line, column);
                return null;
            }
            */

            string accessorFullName = $"{definition.Namespace}.{this._className}";
            string definitionName = definition.DefinitionName;
            bool isAsync = definition.Async;
            bool hasRefParameters = definition.Parameters.Any(x => x.IsOutput);
            ActionDefinitionTarget actionTarget = new LocalActionTarget(accessorFullName, definitionName, isAsync, hasRefParameters);
            actionDefinition = new ActionDefinition(actionTarget);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
            foreach (SqlQueryParameter parameter in definition.Parameters)
            {
                base.CollectActionParameter
                (
                    parameter.Name
                  , parameter.Type
                  , parameter.DefaultValue
                  , parameter.IsOutput
                  , targetName
                  , filePath
                  , line
                  , column
                  , parameterRegistry
                  , explicitParameters
                  , pathParameters
                  , bodyParameters
                );
            }

            foreach (ErrorResponse errorResponse in definition.ErrorResponses)
                RegisterErrorResponse(actionDefinition, errorResponse.StatusCode, errorResponse.ErrorCode, errorResponse.ErrorDescription);

            CollectResponse(actionDefinition, definition);
            return true;
        }
        #endregion

        #region Private Methods
        private static void CollectResponse(ActionDefinition actionDefinition, SqlStatementDefinition definition)
        {
            if (definition.FileResult != null)
                actionDefinition.SetFileResponse(new ActionFileResponse(HttpMediaType.Binary), definition.FileResult.Source, definition.FileResult.Line, definition.FileResult.Column);
            else
                actionDefinition.DefaultResponseType = definition.ResultType;
        }
        #endregion
    }
}