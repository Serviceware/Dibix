using System;
using System.Collections.Generic;
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
        #endregion

        #region Protected Methods
        protected internal void ParseContent(TSqlStatement content, StatementList statements)
        {
            this.Target.Namespace = content.SingleHint(SqlHint.Namespace);
            string name = content.SingleHint(SqlHint.Name);
            if (!String.IsNullOrEmpty(name))
                this.Target.Name = name;

            this.Target.ResultTypeName = content.SingleHint(SqlHint.ResultTypeName);

            this.ParseResults(content);
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
            if (previousToken.TokenType == TSqlTokenType.WhiteSpace)
                previousToken = node.ScriptTokenStream[--startIndex];

            if (previousToken.TokenType == TSqlTokenType.MultilineComment)
                parameter.ClrTypeName = node.SingleHint(SqlHint.ClrType, startIndex);

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
                bool shouldBeNullable = node.IsSet(SqlHint.Nullable, startIndex);
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

        private void ParseResults(TSqlStatement node)
        {
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Target, node, this.ContractResolverFacade, this.ErrorReporter);
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