using System.Collections.Generic;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRule
    {
        int Id { get; }

        IEnumerable<SqlRuleProblem> Analyze(SqlRuleExecutionContext context);
    }
}