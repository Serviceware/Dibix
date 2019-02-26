using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IWriter
    {
        string Write(string projectName, string @namespace, string className, SqlQueryOutputFormatting formatting, IList<SqlStatementInfo> statements);
    }
}