using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRule
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(SqlCodeAnalysisContext context);
    }
}