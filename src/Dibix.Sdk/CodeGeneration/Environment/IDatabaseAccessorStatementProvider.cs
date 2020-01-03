using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IDatabaseAccessorStatementProvider
    {
        IEnumerable<SqlStatementInfo> CollectStatements(IEnumerable<string> sources);
    }
}