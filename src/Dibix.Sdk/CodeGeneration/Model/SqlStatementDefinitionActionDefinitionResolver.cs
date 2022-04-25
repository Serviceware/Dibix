using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.Model;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a SQL statement within the current project or an external project
    internal sealed class SqlStatementDefinitionActionDefinitionResolver : ActionDefinitionResolver
    {
        #region Fields
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _className;
        private readonly ISqlStatementDefinitionProvider _sqlStatementDefinitionProvider;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly IDictionary<string, SqlStatementDefinition> _externalDefinitions;
        #endregion

        #region Constructor
        public SqlStatementDefinitionActionDefinitionResolver
        (
            string rootNamespace
          , string productName
          , string areaName
          , string className
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        ) : base(schemaRegistry, logger)
        {
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._className = className;
            this._sqlStatementDefinitionProvider = sqlStatementDefinitionProvider;
            this._externalSchemaResolver = externalSchemaResolver;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._externalDefinitions = new Dictionary<string, SqlStatementDefinition>();
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
            string fullName = $"{normalizedNamespace}.{methodName}";

            if (!this.TryGetLocalStatementDefinition(fullName, out SqlStatementDefinition statementDefinition, out string accessorClassName)
             && !this.TryGetExternalStatementDefinition(fullName, out statementDefinition, out accessorClassName))
            {
                actionDefinition = null;
                return false;
            }

            string localAccessorFullName = $"{statementDefinition.Namespace}.{this._className}";
            string externalAccessorFullName = $"{statementDefinition.Namespace}.{accessorClassName}";
            string definitionName = statementDefinition.DefinitionName;
            bool isAsync = statementDefinition.Async;
            bool hasRefParameters = statementDefinition.Parameters.Any(x => x.IsOutput);
            ActionDefinitionTarget actionTarget = new LocalActionTarget(localAccessorFullName, externalAccessorFullName, definitionName, isAsync, hasRefParameters, filePath, line, column);
            actionDefinition = new ActionDefinition(actionTarget);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionDefinition, pathParameters);
            foreach (SqlQueryParameter parameter in statementDefinition.Parameters)
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

            foreach (ErrorResponse errorResponse in statementDefinition.ErrorResponses)
                RegisterErrorResponse(actionDefinition, errorResponse.StatusCode, errorResponse.ErrorCode, errorResponse.ErrorDescription);

            CollectResponse(actionDefinition, statementDefinition);
            return true;
        }
        #endregion

        #region Private Methods
        private bool TryGetLocalStatementDefinition(string fullName, out SqlStatementDefinition statementDefinition, out string accessorClassName)
        {
            if (this._sqlStatementDefinitionProvider.TryGetDefinition(fullName, out statementDefinition))
            {
                accessorClassName = this._className;
                return true;
            }

            accessorClassName = null;
            return false;
        }

        private bool TryGetExternalStatementDefinition(string fullName, out SqlStatementDefinition statementDefinition, out string accessorClassName)
        {
            if (this._externalSchemaResolver.TryGetSchema(fullName, out ExternalSchemaDefinition externalSchemaDefinition))
            {
                statementDefinition = externalSchemaDefinition.GetSchema<SqlStatementDefinition>();
                accessorClassName = externalSchemaDefinition.Owner.DefaultClassName;
                return true;
            }

            statementDefinition = null;
            accessorClassName = null;
            return false;
        }

        private bool TryGetExternalStatementDefinitionLazy(string fullName, out SqlStatementDefinition statementDefinition)
        {
            if (this._externalDefinitions.TryGetValue(fullName, out statementDefinition))
                return true;

            SqlStatementDefinition matchingDefinition = this._referencedAssemblyInspector.Inspect(referencedAssemblies =>
            {
                var query = from assembly in referencedAssemblies
                            let model = CodeGenerationModelSerializer.Read(assembly)
                            from statement in model.SqlStatements
                            where statement.FullName == fullName
                            select statement;

                return query.FirstOrDefault();
            });

            if (matchingDefinition == null) 
                return false;

            base.SchemaRegistry.Populate(matchingDefinition);
            this._externalDefinitions.Add(fullName, matchingDefinition);
            statementDefinition = matchingDefinition;
            return true;
        }

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