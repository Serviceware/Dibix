using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    internal delegate void ReportSqlCodeAnalysisError(TSqlFragment fragment, TSqlParserToken token, params object[] args);
}