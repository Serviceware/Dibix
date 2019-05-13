using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypeWriter : IDaoWriter
    {
        #region Properties
        public string RegionName => "Structured types";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(SourceArtifacts artifacts) => artifacts.Contracts.Any();

        public void Write(DaoWriterContext context)
        {
        }
        #endregion

        #region Private Methods
        #endregion
    }
}