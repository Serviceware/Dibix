using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IWriter
    {
        string Namespace { get; set; }
        string ClassName { get; set; }
        SqlQueryOutputFormatting Formatting { get; set; }

        string Write(string projectName, IList<SqlStatementInfo> statements);
    }
}