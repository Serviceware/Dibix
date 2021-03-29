using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            base.Target.ProcedureName = String.Join(".", node.ProcedureReference.Name.Identifiers.Select(x => Identifier.EncodeIdentifier(x.Value)));

            StatementList statements = node.StatementList ?? new StatementList();
            this.ParseContent(node, statements);

            foreach (ProcedureParameter parameter in node.Parameters)
                this.ParseParameter(parameter);

            this.ParseErrorResponses(statements);

            base.ExplicitVisit(node);
        }
        #endregion

        #region Private Methods
        private void ParseParameter(ProcedureParameter node)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            ISqlMarkupDeclaration markup = SqlMarkupReader.ReadFragment(node, base.Target.Source, base.Logger);

            SqlQueryParameter parameter = new SqlQueryParameter
            {
                Name = parameterName,
                Type = this.ParseParameterType(parameterName, node, markup),
                IsOutput = node.Modifier == ParameterModifier.Output
            };
            
            this.ParseParameterObfuscate(parameter, markup);
            this.ParseDefaultValue(node, parameter);

            base.Target.Parameters.Add(parameter);
        }

        private TypeReference ParseParameterType(string parameterName, DeclareVariableElement parameter, ISqlMarkupDeclaration markup)
        {
            bool isNullable = parameter.Nullable?.Nullable ?? false;
            return parameter.DataType.ToTypeReference(isNullable, parameterName, base.Target.Namespace, base.Target.Source, markup, base.TypeResolver, base.Logger, out _);
        }

        private void ParseParameterObfuscate(SqlQueryParameter target, ISqlMarkupDeclaration markup)
        {
            target.Obfuscate = markup.TryGetSingleElement(SqlMarkupKey.Obfuscate, base.Target.Source, base.Logger, out ISqlElement _);
        }

        private void ParseDefaultValue(DeclareVariableElement parameter, SqlQueryParameter target)
        {
            if (parameter.Value == null)
                return;

            target.HasDefaultValue = this.TryParseParameterDefaultValue(parameter.Value, target.Type, out object defaultValue);
            target.DefaultValue = defaultValue;
        }

        private bool TryParseParameterDefaultValue(ScalarExpression value, TypeReference targetType, out object defaultValue)
        {
            switch (value)
            {
                case NullLiteral _:
                    defaultValue = null;
                    return true;

                case Literal literal when targetType is PrimitiveTypeReference primitiveTypeReference:
                    return this.TryParseParameterDefaultValue(literal, primitiveTypeReference.Type, out defaultValue);

                case VariableReference variableReference:
                    defaultValue = variableReference.Name;
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Only literals and parameter references are supported for parameter defaults");
            }
        }

        private bool TryParseParameterDefaultValue(Literal literal, PrimitiveType primitiveType, out object defaultValue)
        {
            switch (literal.LiteralType)
            {
                case LiteralType.Integer when primitiveType == PrimitiveType.Boolean:
                    defaultValue = literal.Value == "1";
                    return true;

                case LiteralType.Integer:
                    defaultValue = Int32.Parse(literal.Value);
                    return true;

                case LiteralType.String:
                    defaultValue = literal.Value;
                    return true;

                case LiteralType.Null:
                    defaultValue = null;
                    return true;

                default:
                    base.Logger.LogError(null, $"Literal type not supported for default value: {literal.LiteralType}", base.Target.Source, literal.StartLine, literal.StartColumn);
                    defaultValue = null;
                    return false;
            }
        }

        private void ParseContent(TSqlFragment content, StatementList statements)
        {
            _ = base.Markup.TryGetSingleElementValue(SqlMarkupKey.Namespace, base.Target.Source, base.Logger, out string relativeNamespace);

            base.Target.Namespace = this.ParseNamespace(relativeNamespace);
            base.Target.Name = this.ParseName();
            base.Target.MergeGridResult = base.Markup.HasSingleElement(SqlMarkupKey.MergeGridResult, base.Target.Source, base.Logger);
            base.Target.GenerateInputClass = base.Markup.HasSingleElement(SqlMarkupKey.GenerateInputClass, base.Target.Source, base.Logger);
            base.Target.Async = base.Markup.HasSingleElement(SqlMarkupKey.Async, base.Target.Source, base.Logger);

            this.ParseResults(content, base.Markup);

            bool generateResultClass = false;
            base.Target.ResultType = this.DetermineResultType(relativeNamespace, ref generateResultClass);
            base.Target.GenerateResultClass = generateResultClass;

            this.ParseBody(statements);
        }

        private string ParseNamespace(string relativeNamespace) => NamespaceUtility.BuildAbsoluteNamespace(base.ProductName, base.AreaName, LayerName.Data, relativeNamespace);

        private string ParseName() => base.Markup.TryGetSingleElementValue(SqlMarkupKey.Name, base.Target.Source, base.Logger, out string name) ? name : base.Target.Name;

        private TypeReference DetermineResultType(string relativeNamespace, ref bool generateResultClass)
        {
            // Explicit result type
            if (base.Markup.TryGetSingleElementValue(SqlMarkupKey.ResultTypeName, base.Target.Source, base.Logger, out ISqlElementValue value))
            {
                TypeReference type = base.TypeResolver.ResolveType(value.Value, base.Target.Namespace, base.Target.Source, value.Line, value.Column, false);
                return type;
            }

            if (base.Target.Results.Count == 0)
                return null;

            // Grid result is merged to first result type
            if (base.Target.MergeGridResult)
                return base.Target.Results[0].Types[0];

            // Generate grid result type
            if (base.Target.Results.Any(x => x.Name != null))
            {
                generateResultClass = true;
                return this.GenerateGridResultType(relativeNamespace);
            }

            return base.Target.Results[0].Types[0];
        }

        private TypeReference GenerateGridResultType(string relativeNamespace)
        {
            string gridResultTypeNamespace;
            string gridResultTypeName;

            // Control grid result type name and namespace
            if (base.Markup.TryGetSingleElementValue(SqlMarkupKey.GeneratedResultTypeName, base.Target.Source, base.Logger, out string gridResultTypeNameHint))
            {
                int typeNameIndex = gridResultTypeNameHint.LastIndexOf('.');
                gridResultTypeNamespace = typeNameIndex < 0 ? null : gridResultTypeNameHint.Substring(0, typeNameIndex);
                gridResultTypeName = typeNameIndex < 0 ? gridResultTypeNameHint : gridResultTypeNameHint.Substring(typeNameIndex + 1, gridResultTypeNameHint.Length - typeNameIndex - 1);
            }
            else
            {
                gridResultTypeNamespace = relativeNamespace;

                // Generate type name based on statement name
                gridResultTypeName = $"{base.Target.Name}{GridResultTypeSuffix}";
            }

            string @namespace = NamespaceUtility.BuildAbsoluteNamespace(base.ProductName, base.AreaName, LayerName.DomainModel, gridResultTypeNamespace);
            SchemaTypeReference typeReference = SchemaTypeReference.WithNamespace(@namespace, gridResultTypeName, base.Target.Source, 0, 0, false, false);
            if (base.SchemaRegistry.IsRegistered(typeReference.Key)) 
                return typeReference;

            ObjectSchema schema = new ObjectSchema(@namespace, gridResultTypeName);
            schema.Properties.AddRange(base.Target
                                           .Results
                                           .Select(x => new ObjectSchemaProperty(x.Name, x.ResultType)));
            base.SchemaRegistry.Populate(schema);

            return typeReference;
        }

        private void ParseResults(TSqlFragment node, ISqlMarkupDeclaration markup)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(base.Target, node, base.FragmentAnalyzer, markup, base.TypeResolver, base.SchemaRegistry, base.Logger);
            base.Target.Results.AddRange(results);
        }

        private void ParseBody(StatementList statementList)
        {
            BeginEndBlockStatement beginEndBlock = statementList.Statements.OfType<BeginEndBlockStatement>().FirstOrDefault();
            if (beginEndBlock != null)
                statementList = beginEndBlock.StatementList;

            base.Target.Content = base.Formatter.Format(base.Target, statementList);
        }

        private void ParseErrorResponses(TSqlFragment body)
        {
            ThrowVisitor visitor = new ThrowVisitor();
            body.Accept(visitor);

            base.Target.ErrorResponses.AddRange(visitor.ErrorResponses);
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