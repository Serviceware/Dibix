using System.Data;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExecStoredProcedureSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override string Format(SqlStatementInfo info, StatementList statementList)
        {
            info.CommandType = CommandType.StoredProcedure;
            return info.ProcedureName;
        }
    }
}