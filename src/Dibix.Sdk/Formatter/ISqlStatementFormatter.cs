using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public interface ISqlStatementFormatter
    {
        string Format(SqlStatementInfo info, StatementList statementList);
    }
}