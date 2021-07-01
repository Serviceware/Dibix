using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementFormatter
    {
        bool StripWhiteSpace { get; set; }

        FormattedSqlStatement Format(SqlStatementDescriptor statementDescriptor, StatementList statementList);
    }
}