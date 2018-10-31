using System;
using System.Data;

namespace Dibix
{
    internal static class DbTypeMap
    {
        public static SqlDbType ToSqlDbType(Type clrType)
        {
            Type nullableType = Nullable.GetUnderlyingType(clrType);
            if (nullableType != null)
                clrType = nullableType;

            if (clrType == typeof(bool))
                return SqlDbType.Bit;
            if (clrType == typeof(byte))
                return SqlDbType.TinyInt;
            if (clrType == typeof(byte[]))
                return SqlDbType.VarBinary;
            if (clrType == typeof(char))
                return SqlDbType.Char;
            if (clrType == typeof(DateTime))
                return SqlDbType.DateTime;
            if (clrType == typeof(DateTimeOffset))
                return SqlDbType.DateTimeOffset;
            if (clrType == typeof(decimal))
                return SqlDbType.Decimal;
            if (clrType == typeof(double))
                return SqlDbType.Float;
            if (clrType == typeof(float))
                return SqlDbType.Real;
            if (clrType == typeof(Guid))
                return SqlDbType.UniqueIdentifier;
            if (clrType == typeof(int))
                return SqlDbType.Int;
            if (clrType == typeof(long))
                return SqlDbType.BigInt;
            if (clrType == typeof(short))
                return SqlDbType.SmallInt;
            if (clrType == typeof(string))
                return SqlDbType.NVarChar;
            if (clrType == typeof(TimeSpan))
                return SqlDbType.Time;

            throw new ArgumentOutOfRangeException("clrType", clrType, null);
        }
    }
}