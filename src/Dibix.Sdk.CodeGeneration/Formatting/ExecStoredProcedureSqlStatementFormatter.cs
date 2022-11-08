using System.Data;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExecStoredProcedureSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override FormattedSqlStatement Format(SqlStatementDefinition statementDefinition, StatementList statementList) => new FormattedSqlStatement(statementDefinition.ProcedureName, CommandType.StoredProcedure);
    }
}