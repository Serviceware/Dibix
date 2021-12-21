using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class PrimitiveTypeMap
    {
        private static readonly IDictionary<Type, PrimitiveType> TypeMap = new Dictionary<Type, PrimitiveType>
        {
            [typeof(bool)]           = PrimitiveType.Boolean
          , [typeof(byte)]           = PrimitiveType.Byte
          , [typeof(short)]          = PrimitiveType.Int16
          , [typeof(int)]            = PrimitiveType.Int32
          , [typeof(long)]           = PrimitiveType.Int64
          , [typeof(float)]          = PrimitiveType.Float
          , [typeof(double)]         = PrimitiveType.Double
          , [typeof(decimal)]        = PrimitiveType.Decimal
          , [typeof(byte[])]         = PrimitiveType.Binary
          , [typeof(DateTime)]       = PrimitiveType.DateTime
          , [typeof(DateTimeOffset)] = PrimitiveType.DateTimeOffset
          , [typeof(string)]         = PrimitiveType.String
          , [typeof(Uri)]            = PrimitiveType.Uri
          , [typeof(Guid)]           = PrimitiveType.UUID
        };
        // System.ReflectionOnlyType <> System.RuntimeType
        private static readonly IDictionary<Guid, PrimitiveType> GuidMap = TypeMap.ToDictionary(x => x.Key.GUID, x => x.Value);

        public static bool TryParsePrimitiveType(Type clrType, out PrimitiveType primitiveType) => GuidMap.TryGetValue(clrType.GUID, out primitiveType);
    }
}