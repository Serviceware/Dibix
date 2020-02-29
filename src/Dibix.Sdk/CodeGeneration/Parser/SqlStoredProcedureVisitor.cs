using System;
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

            foreach (ProcedureParameter parameter in node.Parameters)
                this.ParseParameter(parameter);

            this.ParseContent(node, node.StatementList);
            base.ExplicitVisit(node);
        }
        #endregion

        #region Private Methods
        private void ParseParameter(ProcedureParameter node)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            if (node.Modifier == ParameterModifier.Output) 
                this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $"Output parameters are not supported: {parameterName}");

            // Determine parameter type
            SqlQueryParameter parameter = new SqlQueryParameter { Name = parameterName };

            // Parse type name hint
            int startIndex = node.FirstTokenIndex;
            TSqlParserToken previousToken = node.ScriptTokenStream[--startIndex];
            if (previousToken.TokenType == SqlTokenType.WhiteSpace)
                previousToken = node.ScriptTokenStream[--startIndex];

            ICollection<SqlHint> hints = SqlHintParser.FromToken(this.Target.Source, this.ErrorReporter, previousToken).ToArray();
            if (previousToken.TokenType == SqlTokenType.MultilineComment)
                parameter.ClrTypeName = hints.SingleHintValue(SqlHint.ClrType);

            parameter.Obfuscate = hints.IsSet(SqlHint.Obfuscate);
            
            // NOTE: Uncomment line dbx_tests_syntax_empty_params_inputclass line 5, whenever this is implemented
            if (parameter.Obfuscate && this.Target.GenerateInputClass) 
            {
                this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $@"Parameter obfuscation is currently not supported with input classes: {parameter.Name}
Either remove the @GenerateInputClass hint on the statement or the @Obfuscate hint on the parameter");
            }

            parameter.ClrType = node.DataType.ToClrType();
            if (parameter.ClrType == null)
            {
                if (node.DataType is UserDataTypeReference userDataType)
                {
                    parameter.TypeName = $"[{userDataType.Name.SchemaIdentifier.Value}].[{userDataType.Name.BaseIdentifier.Value}]";

                    // Type name hint is the only way to determine the UDT .NET type, that's why it's required
                    if (String.IsNullOrEmpty(parameter.ClrTypeName))
                    {
                        this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $@"Could not determine CLR type for table value parameter
Parameter: {parameter.Name}
UDT type: {parameter.TypeName}
Please mark it with /* @ClrType <ClrTypeName> */");
                    }

                    parameter.Check = ContractCheck.NotNull;
                }
                else
                {
                    throw new InvalidOperationException($@"Unknown data type reference for parameter
Parameter: {parameter.Name}
ReferenceType: {node.DataType.GetType()}");
                }
            }

            if (parameter.ClrType != null)
            {
                bool shouldBeNullable = hints.IsSet(SqlHint.Nullable);
                bool isNullable = parameter.ClrType.IsNullable();
                bool makeNullable = shouldBeNullable && !isNullable;
                if (makeNullable)
                    parameter.ClrType = parameter.ClrType.MakeNullable();

                if (String.IsNullOrEmpty(parameter.ClrTypeName) || makeNullable)
                    parameter.ClrTypeName = parameter.ClrType.ToCSharpTypeName();

                parameter.Check = EvaluateContractCheck(parameter.Check, !shouldBeNullable && isNullable, parameter.ClrType);
            }

            this.Target.Parameters.Add(parameter);
        }

        private static ContractCheck EvaluateContractCheck(ContractCheck currentValue, bool requiresCheck, Type clrType)
        {
            if (currentValue != ContractCheck.None)
                return currentValue;

            if (!requiresCheck)
                return ContractCheck.None;

            if (clrType == typeof(string))
                return ContractCheck.NotNullOrEmpty;

            return ContractCheck.NotNull;
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
            this.Target.ResultType = this.ParseResultType(base.Target.Source);

            this.ParseResults(content, this.Hints);

            this.Target.GridResultType = this.ParseGridResultType(relativeNamespace);

            this.ParseBody(statements ?? new StatementList());
        }

        private Namespace ParseNamespace(string relativeNamespace)
        {
            return Namespace.Create(this.ProductName, this.AreaName, LayerName.Data, relativeNamespace);
        }

        private string ParseName()
        {
            string name = this.Hints.SingleHintValue(SqlHint.Name);
            return !String.IsNullOrEmpty(name) ? name : base.Target.Name;
        }

        private ContractName ParseResultType(string source)
        {
            SqlHint resultTypeHint = this.Hints.SingleHint(SqlHint.ResultTypeName);
            if (resultTypeHint == null) 
                return null;
            
            ContractInfo contract = this.ContractResolver.ResolveContract(resultTypeHint.Value, x => this.ErrorReporter.RegisterError(source, resultTypeHint.Line, resultTypeHint.Column, null, x));
            return contract?.Name;
        }

        private GridResultType ParseGridResultType(string relativeNamespace)
        {
            bool isGridResult = this.Target.Results.Count > 1 && this.Target.ResultType == null && !this.Target.MergeGridResult;
            return isGridResult ? this.GenerateGridResultType(relativeNamespace) : null;
        }

        private GridResultType GenerateGridResultType(string relativeNamespace)
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

            return new GridResultType(Namespace.Create(this.ProductName, this.AreaName, LayerName.DomainModel, gridResultTypeNamespace), gridResultTypeName);
        }

        private void ParseResults(TSqlFragment node, ICollection<SqlHint> hints)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Target, node, this.ElementLocator, hints, this.ContractResolver, this.ErrorReporter);
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