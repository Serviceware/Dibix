using System.Collections.Generic;
using System.Linq;
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
        private readonly ISqlStatementDefinitionProvider _sqlStatementDefinitionProvider;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly IExternalSchemaResolver _externalSchemaResolver;
        private readonly IDictionary<string, SqlStatementDefinition> _externalDefinitions;
        #endregion

        #region Constructor
        public SqlStatementDefinitionActionTargetDefinitionResolver
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
            _productName = productName;
            _areaName = areaName;
            _className = className;
            _sqlStatementDefinitionProvider = sqlStatementDefinitionProvider;
            _externalSchemaResolver = externalSchemaResolver;
            _referencedAssemblyInspector = referencedAssemblyInspector;
            _externalDefinitions = new Dictionary<string, SqlStatementDefinition>();
        }
        #endregion

        #region Overrides
        public override bool TryResolve<T>(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out T actionTargetDefinition)
        {
            if (!TryGetStatementDefinitionByProbing(targetName, out SqlStatementDefinition statementDefinition, out string accessorClassName))
            {
                actionTargetDefinition = null;
                return false;
            }

            string localAccessorFullName = $"{statementDefinition.Namespace}.{_className}";
            string externalAccessorFullName = $"{statementDefinition.Namespace}.{accessorClassName}";
            string definitionName = statementDefinition.DefinitionName;
            bool isAsync = statementDefinition.Async;
            bool hasRefParameters = statementDefinition.Parameters.Any(x => x.IsOutput);
            ActionTarget actionTarget = new LocalActionTarget(statementDefinition, localAccessorFullName, externalAccessorFullName, definitionName, isAsync, hasRefParameters, filePath, line, column);
            actionTargetDefinition = new T();
            actionTargetDefinition.Target = actionTarget;
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
                RegisterErrorResponse(actionTargetDefinition, errorResponse.StatusCode, errorResponse.ErrorCode, errorResponse.ErrorDescription);

            CollectResponse(actionTargetDefinition, statementDefinition);
            return true;
        }
        #endregion

        #region Private Methods
        private bool TryGetStatementDefinitionByProbing(string targetName, out SqlStatementDefinition statementDefinition, out string accessorClassName)
        {
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates(_productName, _areaName, LayerName.Data, relativeNamespace: null, targetName))
            {
                // Try local definition
                if (_sqlStatementDefinitionProvider.TryGetDefinition(candidate, out statementDefinition))
                {
                    accessorClassName = _className;
                    return true;
                }

                // Try external definition
                if (_externalSchemaResolver.TryGetSchema(candidate, out ExternalSchemaDefinition externalSchemaDefinition))
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
            if (_externalDefinitions.TryGetValue(fullName, out statementDefinition))
                return true;

            SqlStatementDefinition matchingDefinition = _referencedAssemblyInspector.Inspect(referencedAssemblies =>
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
            _externalDefinitions.Add(fullName, matchingDefinition);
            statementDefinition = matchingDefinition;
            return true;
        }

        private static void CollectResponse(ActionTargetDefinition actionTargetDefinition, SqlStatementDefinition definition)
        {
            if (definition.FileResult != null)
                actionTargetDefinition.SetFileResponse(new ActionFileResponse(HttpMediaType.Binary), definition.FileResult.Source, definition.FileResult.Line, definition.FileResult.Column);
            else
                actionTargetDefinition.DefaultResponseType = definition.ResultType;
        }
        #endregion
    }
}