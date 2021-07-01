using System.Data;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExecStoredProcedureSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override FormattedSqlStatement Format(SqlStatementDescriptor statementDescriptor, StatementList statementList) => new FormattedSqlStatement(statementDescriptor.ProcedureName, CommandType.StoredProcedure);
    }
}