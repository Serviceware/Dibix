using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementFormatter
    {
        bool StripWhiteSpace { get; set; }

        string Format(SqlStatementInfo info, StatementList statementList);
    }
}