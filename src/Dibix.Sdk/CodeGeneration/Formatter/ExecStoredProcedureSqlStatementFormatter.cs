using System.Data;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExecStoredProcedureSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        public override string Format(SqlStatementInfo info, StatementList body)
        {
            info.CommandType = CommandType.StoredProcedure;

            StringBuilder sb = new StringBuilder();
            sb.Append("[dbo].[")
              .Append(info.ProcedureName)
              .Append(']');

            if (info.Parameters.Any())
            {
                sb.Append(' ');
                for (int i = 0; i < info.Parameters.Count; i++)
                {
                    SqlQueryParameter parameter = info.Parameters[i];
                    sb.Append('@')
                      .Append(parameter.Name);

                    if (i + 1 < info.Parameters.Count)
                        sb.Append(", ");
                }
            }

            return sb.ToString();
        }
    }
}