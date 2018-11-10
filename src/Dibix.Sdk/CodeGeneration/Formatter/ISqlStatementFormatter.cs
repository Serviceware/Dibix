using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementFormatter
    {
        string Format(SqlStatementInfo info, StatementList statementList);
    }
}