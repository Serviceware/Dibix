using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SourceArtifacts
    {
        public IList<SqlStatementInfo> Statements { get; }
        public ICollection<ContractDefinition> Contracts { get; }

        public SourceArtifacts()
        {
            this.Statements = new Collection<SqlStatementInfo>();
            this.Contracts = new Collection<ContractDefinition>();
        }
    }
}
