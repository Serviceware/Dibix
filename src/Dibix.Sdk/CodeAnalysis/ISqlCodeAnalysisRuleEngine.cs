using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRuleEngine
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(TSqlFragment fragment);
        IEnumerable<SqlCodeAnalysisError> Analyze(string scriptFilePath);
        IEnumerable<SqlCodeAnalysisError> Analyze(string scriptFilePath, ISqlCodeAnalysisRule rule);
    }
}