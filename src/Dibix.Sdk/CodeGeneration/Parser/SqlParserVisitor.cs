using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        #region Fields
        private const string GridResultTypeSuffix = "Result";
        #endregion

        #region Properties
        internal string ProductName { get; set; }
        internal string AreaName { get; set; }
        internal ISqlStatementFormatter Formatter { get; set; }
        internal IContractResolverFacade ContractResolver { get; set; }
        internal IErrorReporter ErrorReporter { get; set; }
        internal SqlStatementInfo Target { get; set; }
        internal ICollection<SqlHint> Hints { get; }
        #endregion

        #region Constructor
        protected SqlParserVisitor()
        {
            this.Hints = new Collection<SqlHint>();
        }
        #endregion

        #region Protected Methods
        protected internal void ParseContent(SqlStatementInfo target, TSqlStatement content, StatementList statements)
        {
            string relativeNamespace = this.Hints.SingleHintValue(SqlHint.Namespace);
            this.Target.Namespace = Namespace.Create(this.ProductName, this.AreaName, LayerName.Data, relativeNamespace);
            string name = this.Hints.SingleHintValue(SqlHint.Name);
            if (!String.IsNullOrEmpty(name))
                this.Target.Name = name;

            this.Target.MergeGridResult = this.Hints.IsSet(SqlHint.MergeGridResult);
            this.Target.IsFileApi = this.Hints.IsSet(SqlHint.FileApi);
            this.Target.GenerateInputClass = this.Hints.IsSet(SqlHint.GenerateInputClass);
            this.Target.Async = this.Hints.IsSet(SqlHint.Async);
            SqlHint resultTypeHint = this.Hints.SingleHint(SqlHint.ResultTypeName);
            if (resultTypeHint != null)
            {
                ContractInfo contract = this.ContractResolver.ResolveContract(resultTypeHint.Value, x => this.ErrorReporter.RegisterError(target.Source, resultTypeHint.Line, resultTypeHint.Column, null, x));
                if (contract != null)
                    this.Target.ResultType = contract.Name;
            }

            this.ParseResults(content, this.Hints);
            this.ParseBody(statements ?? new StatementList());

            bool isGridResult = this.Target.Results.Count > 1 && this.Target.ResultType == null && !this.Target.MergeGridResult;
            if (isGridResult)
                this.Target.GridResultType = this.GenerateGridResultType(relativeNamespace);
        }

        protected internal void ParseParameter(ProcedureParameter node)
        {
            string parameterName = node.VariableName.Value.TrimStart('@');

            if (node.Modifier == ParameterModifier.Output)
            {
                this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $"Output parameters are not supported: {parameterName}");
                return;
            }

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
            parameter.ClrType = node.DataType.ToClrType();
            if (parameter.ClrType == null)
            {
                if (node.DataType is UserDataTypeReference userDataType)
                {
                    parameter.TypeName = $"[{userDataType.Name.SchemaIdentifier.Value}].[{userDataType.Name.BaseIdentifier.Value}]";

                    // Type name hint is the only way to determine the UDT .NET type, that's why it's required
                    if (String.IsNullOrEmpty(parameter.ClrTypeName))
                        this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $@"Could not determine CLR type for table value parameter
Parameter name: {parameter.Name}
UDT type: {parameter.TypeName}
Please mark it with /* @ClrType <ClrTypeName> */");

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
        #endregion

        #region Private Methods
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

        private void ParseResults(TSqlStatement node, ICollection<SqlHint> hints)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Target, node, hints, this.ContractResolver, this.ErrorReporter);
            this.Target.Results.AddRange(results);
        }

        private void ParseBody(StatementList statementList)
        {
            BeginEndBlockStatement beginEndBlock = statementList.Statements.OfType<BeginEndBlockStatement>().FirstOrDefault();
            if (beginEndBlock != null)
                statementList = beginEndBlock.StatementList;

            this.Target.Content = this.Formatter.Format(this.Target, statementList);
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
        #endregion
    }
}