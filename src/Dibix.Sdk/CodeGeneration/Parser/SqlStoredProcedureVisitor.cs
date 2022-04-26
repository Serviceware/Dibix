using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Http;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStoredProcedureVisitor : SqlParserVisitor
    {
        #region Fields
        private const string GridResultTypeSuffix = "Result";
        #endregion

        #region Overrides
        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            if (!ValidateMarkup(base.Markup, base.Logger))
                return;

            _ = base.Markup.TryGetSingleElementValue(SqlMarkupKey.Namespace, base.Source, base.Logger, out string relativeNamespace);

            string @namespace = this.ParseNamespace(relativeNamespace);
            string definitionName = this.ParseName();

            SqlStatementDefinition definition = new SqlStatementDefinition(@namespace, definitionName, SchemaDefinitionSource.Defined);
            definition.ProcedureName = String.Join(".", node.ProcedureReference.Name.Identifiers.Select(x => Identifier.EncodeIdentifier(x.Value)));
            definition.MergeGridResult = base.Markup.HasSingleElement(SqlMarkupKey.MergeGridResult, base.Source, base.Logger);
            definition.GenerateInputClass = base.Markup.HasSingleElement(SqlMarkupKey.GenerateInputClass, base.Source, base.Logger);
            definition.Async = base.Markup.HasSingleElement(SqlMarkupKey.Async, base.Source, base.Logger);
            definition.FileResult = base.Markup.TryGetSingleElement(SqlMarkupKey.FileResult, base.Source, base.Logger, out ISqlElement fileResultElement) ? fileResultElement : null;

            StatementList statements = node.StatementList ?? new StatementList();
            CollectParameters(node, definition, relativeNamespace);
            CollectResults(node, base.Markup, definition, relativeNamespace);
            CollectResultType(definition, relativeNamespace);
            CollectErrorResponses(statements, definition);
            CollectBody(statements, definition);

            base.Definition = definition;

            base.ExplicitVisit(node);
        }
        #endregion

        #region Private Methods
        private string ParseNamespace(string relativeNamespace) => NamespaceUtility.BuildAbsoluteNamespace(base.RootNamespace, base.ProductName, base.AreaName, LayerName.Data, relativeNamespace);

        private string ParseName() => base.Markup.TryGetSingleElementValue(SqlMarkupKey.Name, base.Source, base.Logger, out string name) ? name : base.DefinitionName;

        private void CollectParameters(ProcedureStatementBodyBase node, SqlStatementDefinition definition, string relativeNamespace)
        {
            foreach (ProcedureParameter parameter in node.Parameters)
                this.CollectParameter(parameter, definition, relativeNamespace);
        }

        private void CollectParameter(ProcedureParameter node, SqlStatementDefinition definition, string relativeNamespace)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            ISqlMarkupDeclaration markup = SqlMarkupReader.ReadFragment(node, base.Source, base.Logger);

            SqlQueryParameter parameter = new SqlQueryParameter
            {
                Name = parameterName,
                Type = this.ParseParameterType(parameterName, node, markup, relativeNamespace),
                IsOutput = node.Modifier == ParameterModifier.Output
            };
            
            this.CollectParameterObfuscate(parameter, markup);
            this.CollectParameterDefault(node, parameter);

            definition.Parameters.Add(parameter);
        }

        private TypeReference ParseParameterType(string parameterName, DeclareVariableElement parameter, ISqlMarkupDeclaration markup, string relativeNamespace)
        {
            bool isNullable = parameter.Nullable?.Nullable ?? false;
            return parameter.DataType.ToTypeReference(isNullable, parameterName, relativeNamespace, base.Source, markup, base.TypeResolver, base.Logger, out _);
        }

        private void CollectParameterObfuscate(SqlQueryParameter target, ISqlMarkupDeclaration markup)
        {
            target.Obfuscate = markup.TryGetSingleElement(SqlMarkupKey.Obfuscate, base.Source, base.Logger, out ISqlElement _);
        }

        private void CollectParameterDefault(DeclareVariableElement parameter, SqlQueryParameter target)
        {
            if (parameter.Value == null)
                return;
            
            target.DefaultValue = SqlValueReferenceParser.Parse(parameter.VariableName.Value, parameter.Value, target.Type, base.Source, base.Logger);
        }

        private TypeReference DetermineResultType(SqlStatementDefinition definition, string relativeNamespace, ref bool generateResultClass)
        {
            // Explicit result type
            if (base.Markup.TryGetSingleElementValue(SqlMarkupKey.ResultTypeName, base.Source, base.Logger, out ISqlElementValue value))
            {
                TypeReference type = base.TypeResolver.ResolveType(value.Value, relativeNamespace, base.Source, value.Line, value.Column, false);
                return type;
            }

            if (definition.Results.Count == 0)
                return null;

            // Grid result is merged to first result type
            if (definition.MergeGridResult)
                return definition.Results[0].ReturnType;

            // Generate grid result type
            if (definition.Results.Any(x => x.Name != null))
            {
                generateResultClass = true;
                return this.GenerateGridResultType(definition, relativeNamespace);
            }

            return definition.Results[0].ReturnType;
        }

        private TypeReference GenerateGridResultType(SqlStatementDefinition definition, string relativeNamespace)
        {
            string gridResultTypeNamespace;
            string gridResultTypeName;

            // Control grid result type name and namespace
            if (base.Markup.TryGetSingleElementValue(SqlMarkupKey.GeneratedResultTypeName, base.Source, base.Logger, out string gridResultTypeNameHint))
            {
                int typeNameIndex = gridResultTypeNameHint.LastIndexOf('.');
                gridResultTypeNamespace = typeNameIndex < 0 ? null : gridResultTypeNameHint.Substring(0, typeNameIndex);
                gridResultTypeName = typeNameIndex < 0 ? gridResultTypeNameHint : gridResultTypeNameHint.Substring(typeNameIndex + 1, gridResultTypeNameHint.Length - typeNameIndex - 1);
            }
            else
            {
                gridResultTypeNamespace = relativeNamespace;

                // Generate type name based on statement name
                gridResultTypeName = $"{definition.DefinitionName}{GridResultTypeSuffix}";
            }

            string @namespace = NamespaceUtility.BuildAbsoluteNamespace(base.RootNamespace, base.ProductName, base.AreaName, LayerName.DomainModel, gridResultTypeNamespace);
            SchemaTypeReference typeReference = SchemaTypeReference.WithNamespace(@namespace, gridResultTypeName, base.Source, 0, 0, false, false);
            if (base.SchemaRegistry.IsRegistered(typeReference.Key)) 
                return typeReference;

            ObjectSchema schema = new ObjectSchema(@namespace, gridResultTypeName, SchemaDefinitionSource.AutoGenerated);
            schema.Properties.AddRange(definition.Results.Select(x => new ObjectSchemaProperty(x.Name, x.ReturnType)));
            base.SchemaRegistry.Populate(schema);

            return typeReference;
        }

        private void CollectResults(TSqlFragment node, ISqlMarkupDeclaration markup, SqlStatementDefinition definition, string relativeNamespace)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(definition, node, base.Source, base.FragmentAnalyzer, markup, relativeNamespace, base.TypeResolver, base.SchemaRegistry, base.Logger);
            definition.Results.AddRange(results);
        }

        private void CollectResultType(SqlStatementDefinition definition, string relativeNamespace)
        {
            bool generateResultClass = false;
            definition.ResultType = this.DetermineResultType(definition, relativeNamespace, ref generateResultClass);
            definition.GenerateResultClass = generateResultClass;
        }

        private static void CollectErrorResponses(TSqlFragment body, SqlStatementDefinition definition)
        {
            ThrowVisitor visitor = new ThrowVisitor();
            body.Accept(visitor);

            definition.ErrorResponses.AddRange(visitor.ErrorResponses);
        }

        private void CollectBody(StatementList statementList, SqlStatementDefinition definition)
        {
            BeginEndBlockStatement beginEndBlock = statementList.Statements.OfType<BeginEndBlockStatement>().FirstOrDefault();
            if (beginEndBlock != null)
                statementList = beginEndBlock.StatementList;

            definition.Statement = base.Formatter.Format(definition, statementList);
        }

        private static bool ValidateMarkup(ISqlMarkupDeclaration markup, ILogger logger)
        {
            foreach (string elementName in markup.ElementNames)
            {
                if (SqlMarkupKey.IsDefined(elementName))
                    continue;

                foreach (ISqlElement element in markup.GetElements(elementName))
                {
                    logger.LogError($"Unexpected markup element '{elementName}'", element.Source, element.Line, element.Column);
                }
            }

            return true;
        }
        #endregion

        #region Nested Types
        private sealed class ThrowVisitor : TSqlFragmentVisitor
        {
            private readonly IDictionary<ErrorResponseKey, ErrorResponse> _errorResponses = new Dictionary<ErrorResponseKey, ErrorResponse>();

            public ICollection<ErrorResponse> ErrorResponses => this._errorResponses.Values;

            public override void Visit(ThrowStatement node)
            {
                string errorMessage = String.Empty;
                if (node.Message is StringLiteral literal)
                    errorMessage = literal.Value;

                if (node.ErrorNumber is Literal errorNumberLiteral
                 && Int32.TryParse(errorNumberLiteral.Value, out int errorNumber)
                 && HttpErrorResponseParser.TryParseErrorResponse(errorNumber, out int statusCode, out int errorCode, out bool isClientError)
                 && isClientError)
                {
                    ErrorResponseKey errorResponseKey = new ErrorResponseKey(statusCode, errorCode);
                    if (this._errorResponses.ContainsKey(errorResponseKey))
                        return;

                    this._errorResponses.Add(errorResponseKey, new ErrorResponse(statusCode, errorCode, errorMessage));
                }
            }
        }

        private readonly struct ErrorResponseKey
        {
            public int StatusCode { get; }
            public int ErrorCode { get; }

            public ErrorResponseKey(int statusCode, int errorCode)
            {
                this.StatusCode = statusCode;
                this.ErrorCode = errorCode;
            }
        }
        #endregion
    }
}