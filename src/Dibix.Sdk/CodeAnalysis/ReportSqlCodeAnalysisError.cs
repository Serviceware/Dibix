using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    internal delegate void ReportSqlCodeAnalysisError(TSqlFragment fragment, int line, int column, params object[] args);
}