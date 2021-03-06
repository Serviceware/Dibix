using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementCollector
    {
        public abstract IEnumerable<SqlStatementDefinition> CollectStatements();
    }
}