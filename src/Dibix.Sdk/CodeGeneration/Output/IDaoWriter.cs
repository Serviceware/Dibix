using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IDaoWriter
    {
        string RegionName { get; }

        bool HasContent(IEnumerable<SqlStatementInfo> context);
        void Write(DaoWriterContext context);
    }
}