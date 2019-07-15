using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IDaoWriter
    {
        string RegionName { get; }

        bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts);
        IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration);
        void Write(DaoWriterContext context);
    }
}