using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class PrimaryKeyDataType
    {
        public static ICollection<SqlDataType> AllowedTypes { get; } = new HashSet<SqlDataType>
        {
            SqlDataType.TinyInt
          , SqlDataType.SmallInt
          , SqlDataType.Int
          , SqlDataType.BigInt
          , SqlDataType.Date
        };
    }
}
