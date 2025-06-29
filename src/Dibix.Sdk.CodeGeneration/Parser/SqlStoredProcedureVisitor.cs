﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dibix.Http;
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
            ISqlMarkupDeclaration markup = SqlMarkupReader.Read(node, SqlMarkupCommentKind.SingleLine, Source, Logger);
            bool hasMarkup = markup.HasElements;
            bool hasNoCompileElement = markup.HasSingleElement(SqlMarkupKey.NoCompile, Source, Logger);

            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !base.IsEmbedded;

            bool include = (!requireExplicitMarkup || hasMarkup) && !hasNoCompileElement;
            if (!include)
                return;

            _ = markup.TryGetSingleElementValue(SqlMarkupKey.Namespace, base.Source, base.Logger, out string relativeNamespace);

            NamespacePath @namespace = ParseNamespace(relativeNamespace);
            string definitionName = ParseName(markup);

            SqlStatementDefinition definition = new SqlStatementDefinition(@namespace.Path, @namespace.RelativeNamespace, definitionName, SchemaDefinitionSource.Defined, new SourceLocation(Source, node.StartLine, node.StartColumn));
            definition.ProcedureName = String.Join(".", node.ProcedureReference.Name.Identifiers.Select(x => Identifier.EncodeIdentifier(x.Value)));
            definition.MergeGridResult = markup.HasSingleElement(SqlMarkupKey.MergeGridResult, base.Source, base.Logger);
            definition.GenerateInputClass = markup.HasSingleElement(SqlMarkupKey.GenerateInputClass, base.Source, base.Logger);
            definition.Async = markup.HasSingleElement(SqlMarkupKey.Async, base.Source, base.Logger);
            definition.FileResult = markup.TryGetSingleElement(SqlMarkupKey.FileResult, base.Source, base.Logger, out ISqlElement fileResultElement) ? fileResultElement : null;

            StatementList statements = node.StatementList ?? new StatementList();
            CollectParameters(node, definition, relativeNamespace);
            CollectResults(node, definition, markup, relativeNamespace);
            CollectResultType(definition, relativeNamespace, markup);
            CollectErrorResponses(statements, definition);
            CollectBody(statements, definition);

            SetDefinition(definition);
        }
        #endregion

        #region Private Methods
        private NamespacePath ParseNamespace(string relativeNamespace) => PathUtility.BuildAbsoluteNamespace(base.ProductName, base.AreaName, LayerName.Data, relativeNamespace);

        private string ParseName(ISqlMarkupDeclaration markup) => markup.TryGetSingleElementValue(SqlMarkupKey.Name, base.Source, base.Logger, out string name) ? name : base.DefinitionName;

        private void CollectParameters(ProcedureStatementBodyBase node, SqlStatementDefinition definition, string relativeNamespace)
        {
            foreach (ProcedureParameter parameter in node.Parameters)
                CollectParameter(parameter, definition, relativeNamespace);
        }

        private void CollectParameter(ProcedureParameter node, SqlStatementDefinition definition, string relativeNamespace)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            ISqlMarkupDeclaration markup = SqlMarkupReader.Read(node, SqlMarkupCommentKind.MultiLine, Source, Logger);

            SqlQueryParameter parameter = new SqlQueryParameter
            {
                Name = parameterName,
                Type = ParseParameterType(parameterName, node, markup, relativeNamespace),
                IsOutput = node.Modifier == ParameterModifier.Output
            };

            CollectParameterObfuscate(parameter, markup);
            CollectParameterDefault(node, parameter);

            definition.Parameters.Add(parameter);
        }

        private TypeReference ParseParameterType(string parameterName, DeclareVariableElement parameter, ISqlMarkupDeclaration markup, string relativeNamespace)
        {
            bool isNullable = parameter.Nullable?.Nullable ?? false;
            return parameter.DataType.ToTypeReference(isNullable, parameterName, relativeNamespace, base.Source, markup, base.TypeResolver, base.Logger, out _);
        }

        private void CollectParameterObfuscate(SqlQueryParameter target, ISqlMarkupDeclaration markup)
        {
            target.Obfuscate = markup.HasSingleElement(SqlMarkupKey.Obfuscate, base.Source, base.Logger);
        }

        private void CollectParameterDefault(DeclareVariableElement parameter, SqlQueryParameter target)
        {
            if (parameter.Value == null)
                return;

            target.DefaultValue = SqlValueReferenceParser.Parse(parameter.Value, target.Type, Source, SchemaRegistry, Logger);
        }

        private TypeReference DetermineResultType(SqlStatementDefinition definition, string relativeNamespace, ISqlMarkupDeclaration markup, ref bool generateResultClass)
        {
            // Explicit result type
            if (markup.TryGetSingleElementValue(SqlMarkupKey.ResultTypeName, base.Source, base.Logger, out Token<string> value))
            {
                TypeReference type = base.TypeResolver.ResolveType(value.Value, relativeNamespace, value.Location, false);
                return type;
            }

            if (definition.Results.Count == 0)
                return null;

            // Grid result is merged to first result type
            if (definition.MergeGridResult)
                return definition.Results[0].ReturnType;

            // Generate grid result type
            if (definition.IsGridResult())
            {
                generateResultClass = true;
                return GenerateGridResultType(definition, relativeNamespace, markup);
            }

            return definition.Results[0].ReturnType;
        }

        private TypeReference GenerateGridResultType(SqlStatementDefinition definition, string relativeNamespace, ISqlMarkupDeclaration markup)
        {
            // Control grid result type name and namespace
            TargetPath targetPath;
            if (markup.TryGetSingleElementValue(SqlMarkupKey.GeneratedResultTypeName, base.Source, base.Logger, out Token<string> gridResultTypeNameHint))
            {
                targetPath = PathUtility.BuildAbsoluteTargetName(base.ProductName, base.AreaName, LayerName.DomainModel, relativeNamespace, targetNamePath: gridResultTypeNameHint);
            }
            else
            {
                string generatedTypeName = $"{definition.DefinitionName}{GridResultTypeSuffix}";
                targetPath = PathUtility.BuildAbsoluteTargetName(base.ProductName, base.AreaName, LayerName.DomainModel, relativeNamespace, targetNamePath: generatedTypeName);
            }

            SourceLocation location = new SourceLocation(base.Source, line: 0, column: 0);
            SchemaTypeReference typeReference = new SchemaTypeReference(key: targetPath.Path, isNullable: false, isEnumerable: false, location);
            if (base.SchemaRegistry.IsRegistered(typeReference.Key))
                return typeReference;

            IList<ObjectSchemaProperty> properties = definition.Results
                                                               .Select(x => new ObjectSchemaProperty(x.Name, x.ReturnType))
                                                               .ToArray();
            ObjectSchema schema = new ObjectSchema(targetPath.AbsoluteNamespace, targetPath.RelativeNamespace, targetPath.TargetName, SchemaDefinitionSource.AutoGenerated, location, properties);
            base.SchemaRegistry.Populate(schema);

            return typeReference;
        }

        private void CollectResults(TSqlFragment node, SqlStatementDefinition definition, ISqlMarkupDeclaration markup, string relativeNamespace)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(definition, node, base.Source, base.FragmentAnalyzer, markup, relativeNamespace, base.TypeResolver, base.SchemaRegistry, base.Logger);
            definition.Results.AddRange(results);
        }

        private void CollectResultType(SqlStatementDefinition definition, string relativeNamespace, ISqlMarkupDeclaration markup)
        {
            bool generateResultClass = false;
            definition.ResultType = DetermineResultType(definition, relativeNamespace, markup, ref generateResultClass);
            definition.GenerateResultClass = generateResultClass;
        }

        private static void CollectErrorResponses(TSqlFragment body, SqlStatementDefinition definition)
        {
            ThrowVisitor visitor = new ThrowVisitor(definition.Location.Source, definition.Parameters);
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
        #endregion

        #region Nested Types
        private sealed class ThrowVisitor : TSqlFragmentVisitor
        {
            private readonly string _source;
            private readonly IDictionary<ErrorResponseKey, ErrorResponse> _errorResponses = new Dictionary<ErrorResponseKey, ErrorResponse>();
            private readonly IDictionary<string, string> _variableValues = new Dictionary<string, string>();
            private readonly HashSet<string> _parameters;

            public ICollection<ErrorResponse> ErrorResponses => _errorResponses.Values;

            public ThrowVisitor(string source, IEnumerable<SqlQueryParameter> parameters)
            {
                _source = source;
                _parameters = [..parameters.Select(x => $"@{x.Name}")];
            }

            public override void Visit(DeclareVariableElement node)
            {
                string variableName = node.VariableName.Value;
                CollectVariableAssignment(variableName, node.Value);
            }

            public override void Visit(SetVariableStatement node)
            {
                string variableName = node.Variable.Name;
                CollectVariableAssignment(variableName, node.Expression);
            }

            public override void Visit(ThrowStatement node)
            {
                string errorMessage = node.Message switch
                {
                    StringLiteral literal => literal.Value,
                    VariableReference variableReference when _variableValues.TryGetValue(variableReference.Name, out string variableValue) => variableValue,
                    _ => String.Empty
                };

                if (node.ErrorNumber is not Literal errorNumberLiteral)
                    return;

                if (!Int32.TryParse(errorNumberLiteral.Value, out int errorNumber))
                    return;

                if (!HttpErrorResponseUtility.TryParseErrorResponse(errorNumber, out int statusCode, out int errorCode, out bool isClientError))
                    return;

                if (!isClientError)
                    return;

                ErrorResponseKey errorResponseKey = new ErrorResponseKey(statusCode, errorCode);
                if (_errorResponses.ContainsKey(errorResponseKey))
                    return;

                _errorResponses.Add(errorResponseKey, new ErrorResponse(statusCode, errorCode, errorMessage, new SourceLocation(_source, errorNumberLiteral.StartLine, errorNumberLiteral.StartColumn)));
            }

            private void CollectVariableAssignment(string variableName, ScalarExpression value)
            {
                string variableValue = null;

                switch (value)
                {
                    case null:
                        variableValue = "";
                        break;

                    case StringLiteral literal:
                        variableValue = literal.Value;
                        break;

                    case FunctionCall functionCall when functionCall.FunctionName.Value == "CONCAT":
                        StringBuilder sb = new StringBuilder();

                        foreach (ScalarExpression parameter in functionCall.Parameters)
                        {
                            switch (parameter)
                            {
                                case StringLiteral stringLiteral:
                                    sb.Append(stringLiteral.Value);
                                    break;

                                case VariableReference variableReference when _variableValues.TryGetValue(variableReference.Name, out string variableReferenceValue):
                                    sb.Append(variableReferenceValue);
                                    break;

                                case VariableReference variableReference when _parameters.Contains(variableReference.Name):
                                    sb.Append($"{{{variableReference.Name.TrimStart('@')}}}");
                                    break;

                                default:
                                    continue;
                            }
                        }

                        if (sb.Length > 0)
                            variableValue = sb.ToString();

                        break;

                    default:
                        return;
                }

                if (variableValue == null)
                    return;

                _variableValues[variableName] = variableValue;
            }
        }

        private readonly record struct ErrorResponseKey
        {
            public int StatusCode { get; }
            public int ErrorCode { get; }

            public ErrorResponseKey(int statusCode, int errorCode)
            {
                StatusCode = statusCode;
                ErrorCode = errorCode;
            }
        }
        #endregion
    }
}