using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a SQL statement within the current project or an external project
    internal sealed class SqlStatementDefinitionActionDefinitionResolver : ActionDefinitionResolver
    {
        #region Fields
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
            string productName
          , string areaName
          , string className
          , ISqlStatementDefinitionProvider sqlStatementDefinitionProvider
          , IExternalSchemaResolver externalSchemaResolver
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        ) : base(schemaDefinitionResolver, schemaRegistry, logger)
        {
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
            if (!this.TryGetStatementDefinitionByProbing(targetName, out SqlStatementDefinition statementDefinition, out string accessorClassName))
            {
                actionDefinition = null;
                return false;
            }

            string localAccessorFullName = $"{statementDefinition.Namespace}.{this._className}";
            string externalAccessorFullName = $"{statementDefinition.Namespace}.{accessorClassName}";
            string definitionName = statementDefinition.DefinitionName;
            bool isAsync = statementDefinition.Async;
            bool hasRefParameters = statementDefinition.Parameters.Any(x => x.IsOutput);
            ActionDefinitionTarget actionTarget = new LocalActionTarget(statementDefinition, localAccessorFullName, externalAccessorFullName, definitionName, isAsync, hasRefParameters, filePath, line, column);
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
        private bool TryGetStatementDefinitionByProbing(string targetName, out SqlStatementDefinition statementDefinition, out string accessorClassName)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(this._productName, this._areaName, LayerName.Data, relativeNamespace: null, targetName))
            {
                // Try local definition
                if (this._sqlStatementDefinitionProvider.TryGetDefinition(candidate, out statementDefinition))
                {
                    accessorClassName = this._className;
                    return true;
                }

                // Try external definition
                if (this._externalSchemaResolver.TryGetSchema(candidate, out ExternalSchemaDefinition externalSchemaDefinition))
                {
                    statementDefinition = externalSchemaDefinition.GetSchema<SqlStatementDefinition>();
                    accessorClassName = externalSchemaDefinition.Owner.DefaultClassName;
                    return true;
                }
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