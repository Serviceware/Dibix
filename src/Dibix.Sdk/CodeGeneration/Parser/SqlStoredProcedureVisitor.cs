﻿using System;
using System.Collections.Generic;
using System.Linq;
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

            this.ParseContent(node, node.StatementList);

            foreach (ProcedureParameter parameter in node.Parameters)
                this.ParseParameter(parameter);

            base.ExplicitVisit(node);
        }
        #endregion

        #region Private Methods
        private void ParseParameter(ProcedureParameter node)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            if (node.Modifier == ParameterModifier.Output) 
                this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $"Output parameters are not supported: {parameterName}");

            // Parse type name hint
            int startIndex = node.FirstTokenIndex;
            TSqlParserToken previousToken = node.ScriptTokenStream[--startIndex];
            if (previousToken.TokenType == SqlTokenType.WhiteSpace)
                previousToken = node.ScriptTokenStream[--startIndex];

            ICollection<SqlHint> hints = SqlHintParser.FromToken(this.Target.Source, this.ErrorReporter, previousToken).ToArray();

            SqlQueryParameter parameter = new SqlQueryParameter { Name = parameterName };
            
            this.ParseParameterObfuscate(node, parameter, hints);
            this.ParseParameterType(node, parameter, hints);
            this.ParseDefaultValue(node, parameter);

            this.Target.Parameters.Add(parameter);
        }

        private void ParseParameterType(DeclareVariableElement parameter, SqlQueryParameter target, IEnumerable<SqlHint> hints)
        {
            bool isNullable = parameter.Nullable?.Nullable ?? false;
            target.Type = parameter.DataType.ToTypeReference(isNullable, target.Name, this.Target.Namespace, this.Target.Source, hints, base.TypeResolver, base.ErrorReporter, out string udtTypeName);
            target.UdtTypeName = udtTypeName;
        }

        private void ParseParameterObfuscate(TSqlFragment parameter, SqlQueryParameter target, IEnumerable<SqlHint> hints)
        {
            target.Obfuscate = hints.IsSet(SqlHint.Obfuscate);

            // NOTE: Uncomment line dbx_tests_syntax_empty_params_inputclass line 5, whenever this is implemented
            if (target.Obfuscate && this.Target.GenerateInputClass)
            {
                this.ErrorReporter.RegisterError(this.Target.Source, parameter.StartLine, parameter.StartColumn, null, $@"Parameter obfuscation is currently not supported with input classes: {target.Name}
Either remove the @GenerateInputClass hint on the statement or the @Obfuscate hint on the parameter");
            }
        }

        private void ParseDefaultValue(DeclareVariableElement parameter, SqlQueryParameter target)
        {
            if (parameter.Value == null)
                return;

            if (!(parameter.Value is Literal literal))
            {
                base.ErrorReporter.RegisterError(this.Target.Source, parameter.Value.StartLine, parameter.Value.StartColumn, null, $"Only literals are supported for default values: {parameter.Value.Dump()}");
                return;
            }

            if (!(target.Type is PrimitiveTypeReference primitiveTypeReference))
                throw new InvalidOperationException($@"Unexpected parameter type for default value
Parameter: {target.Name}
DataType: {target.Type.GetType()}");

            target.HasDefaultValue = this.TryParseParameterDefaultValue(literal, primitiveTypeReference.Type, out object defaultValue);
            target.DefaultValue = defaultValue;
        }

        private bool TryParseParameterDefaultValue(Literal literal, PrimitiveDataType targetType, out object defaultValue)
        {
            switch (literal.LiteralType)
            {
                case LiteralType.Integer when targetType == PrimitiveDataType.Boolean:
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
                    base.ErrorReporter.RegisterError(this.Target.Source, literal.StartLine, literal.StartColumn, null, $"Literal type not supported for default value: {literal.LiteralType}");
                    defaultValue = null;
                    return false;
            }
        }

        private void ParseContent(TSqlFragment content, StatementList statements)
        {
            string relativeNamespace = this.Hints.SingleHintValue(SqlHint.Namespace);

            this.Target.Namespace = this.ParseNamespace(relativeNamespace);
            this.Target.Name = this.ParseName();
            this.Target.MergeGridResult = this.Hints.IsSet(SqlHint.MergeGridResult);
            this.Target.IsFileApi = this.Hints.IsSet(SqlHint.FileApi);
            this.Target.GenerateInputClass = this.Hints.IsSet(SqlHint.GenerateInputClass);
            this.Target.Async = this.Hints.IsSet(SqlHint.Async);

            this.ParseResults(content, this.Hints);

            bool generateResultClass = false;
            this.Target.ResultType = this.DetermineResultType(relativeNamespace, ref generateResultClass);
            this.Target.GenerateResultClass = generateResultClass;

            this.ParseBody(statements ?? new StatementList());
        }

        private string ParseNamespace(string relativeNamespace) => NamespaceUtility.BuildAbsoluteNamespace(this.ProductName, this.AreaName, LayerName.Data, relativeNamespace);

        private string ParseName()
        {
            string name = this.Hints.SingleHintValue(SqlHint.Name);
            return !String.IsNullOrEmpty(name) ? name : base.Target.Name;
        }

        private TypeReference DetermineResultType(string relativeNamespace, ref bool generateResultClass)
        {
            SqlHint resultTypeHint = this.Hints.SingleHint(SqlHint.ResultTypeName);
            
            // Explicit result type
            if (resultTypeHint != null)
            {
                TypeReference type = this.TypeResolver.ResolveType(resultTypeHint.Value, this.Target.Namespace, this.Target.Source, resultTypeHint.Line, resultTypeHint.Column, false);
                return type;
            }

            if (this.Target.IsFileApi)
                return null;

            if (this.Target.Results.Count == 0)
                return null;

            // Grid result is merged to first result type
            if (this.Target.MergeGridResult)
                return this.Target.Results[0].Types[0];

            // Generate grid result type
            if (this.Target.Results.Count > 1)
            {
                generateResultClass = true;
                return this.GenerateGridResultType(relativeNamespace);
            }

            return this.Target.Results[0].Types[0];
        }

        private TypeReference GenerateGridResultType(string relativeNamespace)
        {
            string gridResultTypeNamespace;
            string gridResultTypeName;

            // Control grid result type name and namespace
            string gridResultTypeNameHint = this.Hints.SingleHintValue(SqlHint.GeneratedResultTypeName);
            if (gridResultTypeNameHint != null)
            {
                int typeNameIndex = gridResultTypeNameHint.LastIndexOf('.');
                gridResultTypeNamespace = typeNameIndex < 0 ? null : gridResultTypeNameHint.Substring(0, typeNameIndex);
                gridResultTypeName = typeNameIndex < 0 ? gridResultTypeNameHint : gridResultTypeNameHint.Substring(typeNameIndex + 1, gridResultTypeNameHint.Length - typeNameIndex - 1);
            }
            else
            {
                gridResultTypeNamespace = relativeNamespace;

                // Generate type name based on statement name
                gridResultTypeName = $"{this.Target.Name}{GridResultTypeSuffix}";
            }

            string @namespace = NamespaceUtility.BuildAbsoluteNamespace(this.ProductName, this.AreaName, LayerName.DomainModel, gridResultTypeNamespace);
            SchemaTypeReference typeReference = SchemaTypeReference.WithNamespace(@namespace, gridResultTypeName, this.Target.Source, 0, 0, false, false);
            if (base.SchemaRegistry.IsRegistered(typeReference.Key)) 
                return typeReference;

            ObjectSchema schema = new ObjectSchema(@namespace, gridResultTypeName);
            schema.Properties.AddRange(this.Target
                                           .Results
                                           .Select(x => new ObjectSchemaProperty(x.Name, x.ResultType, false, false, SerializationBehavior.Always, false)));
            base.SchemaRegistry.Populate(schema);

            return typeReference;
        }

        private void ParseResults(TSqlFragment node, ICollection<SqlHint> hints)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Target, node, this.ElementLocator, hints, this.TypeResolver, this.SchemaRegistry, this.ErrorReporter);
            this.Target.Results.AddRange(results);
        }

        private void ParseBody(StatementList statementList)
        {
            BeginEndBlockStatement beginEndBlock = statementList.Statements.OfType<BeginEndBlockStatement>().FirstOrDefault();
            if (beginEndBlock != null)
                statementList = beginEndBlock.StatementList;

            this.Target.Content = this.Formatter.Format(this.Target, statementList);
        }
        #endregion
    }
}