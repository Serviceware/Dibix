using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlConstants
    {
        public static readonly HashSet<string> ReservedFunctionNames = new HashSet<string>
        {
            "nodes",   // XML
            "query",   // XML
            "value",   // XML
            "PathName" // FILESTREAM
        };
    }
}
