using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class DaoChildWriterBase : IDaoChildWriter
    {
        public abstract string RegionName { get; }
        public abstract string LayerName { get; }
        public abstract bool HasContent(SourceArtifacts artifacts);
        public virtual IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration) { yield break; }
        public abstract void Write(WriterContext context);
    }
}