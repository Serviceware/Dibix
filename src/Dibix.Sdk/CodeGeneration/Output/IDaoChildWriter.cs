using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IDaoChildWriter
    {
        string RegionName { get; }

        bool HasContent(SourceArtifacts artifacts);
        IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration);
        void Write(WriterContext context);
    }
}