using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISourceSelection
    {
        IEnumerable<SqlStatementInfo> CollectStatements();
    }
}