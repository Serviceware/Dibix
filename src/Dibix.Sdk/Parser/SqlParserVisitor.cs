using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        #region Properties
        internal ISqlStatementFormatter Formatter { get; set; }
        internal IExecutionEnvironment Environment { get; set; }
        internal SqlStatementInfo Target { get; set; }
        #endregion

        #region Protected Methods
        protected internal void ParseContent(TSqlStatement content, StatementList statements)
        {
            string name = content.SingleHint(SqlHint.Name);
            if (!String.IsNullOrEmpty(name))
                this.Target.Name = name;

            this.Target.ResultTypeName = content.SingleHint(SqlHint.ResultTypeName);

            this.ParseResults(content);
            this.ParseBody(statements ?? new StatementList());
        }

        protected internal void ParseParameter(DeclareVariableElement node)
        {
            // Determine parameter type
            SqlQueryParameter parameter = new SqlQueryParameter { Name = node.VariableName.Value.TrimStart('@') };

            // Parse type name hint
            int startIndex = node.FirstTokenIndex;
            TSqlParserToken previousToken = node.ScriptTokenStream[--startIndex];
            if (previousToken.TokenType == TSqlTokenType.WhiteSpace)
                previousToken = node.ScriptTokenStream[--startIndex];

            if (previousToken.TokenType == TSqlTokenType.MultilineComment)
                parameter.ClrTypeName = node.SingleHint(SqlHint.ClrType, startIndex);

            SqlDataTypeReference sqlDataType = node.DataType as SqlDataTypeReference;
            XmlDataTypeReference xmlDataType = node.DataType as XmlDataTypeReference;
            UserDataTypeReference userDataType = node.DataType as UserDataTypeReference;
            if (sqlDataType != null)
            {
                parameter.ClrType = ToClrType(sqlDataType.SqlDataTypeOption);
                parameter.Source = node.SingleHint(SqlHint.Source, startIndex);
            }
            else if (xmlDataType != null)
            {
                parameter.ClrType = typeof(string);
            }
            else if (userDataType != null)
            {
                string name = userDataType.Name.BaseIdentifier.Value;
                if (String.Equals(name, "SYSNAME", StringComparison.OrdinalIgnoreCase))
                    parameter.ClrType = typeof(string);
                else
                {
                    // Type name hint is the only way to determine the UDT .NET type, that's why it's required
                    if (String.IsNullOrEmpty(parameter.ClrTypeName))
                        this.Environment.RegisterError(this.Target.SourcePath, node.StartLine, node.StartColumn, null, $@"Could not determine CLR type for table value parameter
Parameter name: {parameter.Name}
UDT type: {parameter.TypeName}
Please mark it with /* @ClrType <ClrTypeName> */");

                    parameter.TypeName = $"[{userDataType.Name.SchemaIdentifier.Value}].[{userDataType.Name.BaseIdentifier.Value}]";
                    parameter.Check = ContractCheck.NotNull;
                }
            }
            else
            {
                throw new InvalidOperationException($@"Unknown data type reference for parameter
Parameter: {parameter.Name}
ReferenceType: {node.DataType.GetType()}");
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
            IEnumerable<SqlQueryResult> results = StatementOutputParser.Parse(this.Environment, this.Target.SourcePath, node);
            this.Target.Results.AddRange(results);
        }

        private void ParseBody(StatementList statementList)
        {
            BeginEndBlockStatement beginEndBlock = statementList.Statements.OfType<BeginEndBlockStatement>().FirstOrDefault();
            if (beginEndBlock != null)
                statementList = beginEndBlock.StatementList;

            this.Target.Content = this.Formatter.Format(this.Target, statementList);
        }

        private static Type ToClrType(SqlDataTypeOption dataType)
        {
            switch (dataType)
            {
                case SqlDataTypeOption.Bit: return typeof(bool);
                case SqlDataTypeOption.TinyInt: return typeof(byte);
                case SqlDataTypeOption.Binary: return typeof(byte[]);
                case SqlDataTypeOption.VarBinary: return typeof(byte[]);
                case SqlDataTypeOption.Timestamp: return typeof(byte[]);
                case SqlDataTypeOption.Rowversion: return typeof(byte[]);
                case SqlDataTypeOption.Char: return typeof(char);
                case SqlDataTypeOption.DateTime: return typeof(DateTime);
                case SqlDataTypeOption.SmallDateTime: return typeof(DateTime);
                case SqlDataTypeOption.Date: return typeof(DateTime);
                case SqlDataTypeOption.DateTime2: return typeof(DateTime);
                case SqlDataTypeOption.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDataTypeOption.Decimal: return typeof(decimal);
                case SqlDataTypeOption.Numeric: return typeof(decimal);
                case SqlDataTypeOption.Money: return typeof(decimal);
                case SqlDataTypeOption.SmallMoney: return typeof(decimal);
                case SqlDataTypeOption.Float: return typeof(double);
                case SqlDataTypeOption.Real: return typeof(float);
                case SqlDataTypeOption.UniqueIdentifier: return typeof(Guid);
                case SqlDataTypeOption.Int: return typeof(int);
                case SqlDataTypeOption.BigInt: return typeof(long);
                case SqlDataTypeOption.Sql_Variant: return typeof(object);
                case SqlDataTypeOption.SmallInt: return typeof(short);
                case SqlDataTypeOption.VarChar: return typeof(string);
                case SqlDataTypeOption.Text: return typeof(string);
                case SqlDataTypeOption.NChar: return typeof(string);
                case SqlDataTypeOption.NVarChar: return typeof(string);
                case SqlDataTypeOption.NText: return typeof(string);
                case SqlDataTypeOption.Image: return typeof(string);
                case SqlDataTypeOption.Time: return typeof(TimeSpan);
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
        #endregion
    }
}