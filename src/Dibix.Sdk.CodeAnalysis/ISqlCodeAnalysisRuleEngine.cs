using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRuleEngine
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(string source);
        IEnumerable<SqlCodeAnalysisError> Analyze(string source, Type ruleType);
        IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string source, string content);
    }
}