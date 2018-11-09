using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    internal delegate void ReportSqlCodeAnalysisError(TSqlFragment fragment, TSqlParserToken token, params object[] args);
}