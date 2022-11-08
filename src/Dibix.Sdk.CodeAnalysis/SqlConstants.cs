using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlConstants
    {
        public static readonly ICollection<string> ReservedFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "exist",   // XML
            "nodes",   // XML
            "query",   // XML
            "value",   // XML
            "PathName" // FILESTREAM
        };
    }
}