using System.Data;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExecStoredProcedureSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        public override string Format(SqlStatementInfo info, StatementList body)
        {
            info.CommandType = CommandType.StoredProcedure;
            return info.ProcedureName;
        }
    }
}