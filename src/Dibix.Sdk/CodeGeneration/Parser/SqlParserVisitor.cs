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
        #region Properties
        internal ISqlStatementFormatter Formatter { get; set; }
        internal IContractResolverFacade ContractResolverFacade{ get; set; }
        internal IErrorReporter ErrorReporter { get; set; }
        internal SqlStatementInfo Target { get; set; }
        internal ICollection<SqlHint> Hints { get; }
        #endregion

        #region Constructor
        public SqlParserVisitor()
        {
            this.Hints = new Collection<SqlHint>();
        }
        #endregion

        #region Protected Methods
        protected internal void ParseContent(SqlStatementInfo target, TSqlStatement content, StatementList statements)
        {
            this.Target.Namespace = this.Hints.SingleHintValue(SqlHint.Namespace);
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
                ContractInfo contract = this.ContractResolverFacade.ResolveContract(resultTypeHint.Value, x => this.ErrorReporter.RegisterError(target.Source, resultTypeHint.Line, resultTypeHint.Column, null, x));
                if (contract != null)
                    this.Target.ResultType = contract.Name;
            }

            this.Target.GeneratedResultTypeName = this.Hints.SingleHintValue(SqlHint.GeneratedResultTypeName);

            this.ParseResults(content, this.Hints);
            this.ParseBody(statements ?? new StatementList());
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
                    // Type name hint is the only way to determine the UDT .NET type, that's why it's required
                    if (String.IsNullOrEmpty(parameter.ClrTypeName))
                        this.ErrorReporter.RegisterError(this.Target.Source, node.StartLine, node.StartColumn, null, $@"Could not determine CLR type for table value parameter
Parameter name: {parameter.Name}
UDT type: {parameter.TypeName}
Please mark it with /* @ClrType <ClrTypeName> */");

                    parameter.TypeName = $"[{userDataType.Name.SchemaIdentifier.Value}].[{userDataType.Name.BaseIdentifier.Value}]";
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
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Target, node, hints, this.ContractResolverFacade, this.ErrorReporter);
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