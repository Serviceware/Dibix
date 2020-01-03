using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : IDaoChildWriter
    {
        #region Properties
        public string LayerName => CodeGeneration.LayerName.DomainModel;
        public string RegionName => "Contracts";
        #endregion

        #region IDaoChildWriter Members
        public bool HasContent(SourceArtifacts artifacts) => artifacts.Contracts.Any();

        public IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration) { yield break; }

        public void Write(WriterContext context) => ContractCSWriter.Write(context, true);
        #endregion
    }
}