using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRule
    {
        int Id { get; }
        bool IsEnabled { get; }

        IEnumerable<SqlCodeAnalysisError> Analyze(TSqlModel model, TSqlFragment scriptFragment);
    }
}