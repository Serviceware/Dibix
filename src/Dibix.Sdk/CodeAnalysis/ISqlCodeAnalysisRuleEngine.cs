using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRuleEngine
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(TSqlObject modelElement, TSqlFragment scriptFragment);
        IEnumerable<SqlCodeAnalysisError> Analyze(ISqlCodeAnalysisRule rule, string scriptFilePath);
    }
}