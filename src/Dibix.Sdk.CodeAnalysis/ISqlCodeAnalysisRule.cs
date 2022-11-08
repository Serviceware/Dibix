using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRule
    {
        IEnumerable<SqlCodeAnalysisError> Analyze(TSqlFragment fragment);
    }
}