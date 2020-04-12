using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRuleEngine
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(string source);
        IEnumerable<SqlCodeAnalysisError> Analyze(string source, ISqlCodeAnalysisRule rule);
        IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string source, string content);
    }
}