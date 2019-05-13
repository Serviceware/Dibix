using System;
using System.Xml.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal static class SqlDataTypeExtensions
    {
        public static Type ToClrType(this DataTypeReference dataTypeReference)
        {
            SqlDataTypeReference sqlDataType = dataTypeReference as SqlDataTypeReference;
            XmlDataTypeReference xmlDataType = dataTypeReference as XmlDataTypeReference;
            UserDataTypeReference userDataType = dataTypeReference as UserDataTypeReference;
            if (sqlDataType != null)
                return sqlDataType.SqlDataTypeOption.ToClrType();

            if (xmlDataType != null)
                return typeof(XElement);

            if (userDataType != null)
            {
                string name = userDataType.Name.BaseIdentifier.Value;
                if (String.Equals(name, "SYSNAME", StringComparison.OrdinalIgnoreCase))
                    return typeof(string);

                return null;
            }
            return null;
        }

        private static Type ToClrType(this SqlDataTypeOption dataType)
        {
            switch (dataType)
            {
                case SqlDataTypeOption.Bit: return typeof(bool);
                case SqlDataTypeOption.TinyInt: return typeof(byte);
                case SqlDataTypeOption.Binary: return typeof(byte[]);
                case SqlDataTypeOption.VarBinary: return typeof(byte[]);
                case SqlDataTypeOption.Timestamp: return typeof(byte[]);
                case SqlDataTypeOption.Rowversion: return typeof(byte[]);
                case SqlDataTypeOption.Char: return typeof(char);
                case SqlDataTypeOption.DateTime: return typeof(DateTime);
                case SqlDataTypeOption.SmallDateTime: return typeof(DateTime);
                case SqlDataTypeOption.Date: return typeof(DateTime);
                case SqlDataTypeOption.DateTime2: return typeof(DateTime);
                case SqlDataTypeOption.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDataTypeOption.Decimal: return typeof(decimal);
                case SqlDataTypeOption.Numeric: return typeof(decimal);
                case SqlDataTypeOption.Money: return typeof(decimal);
                case SqlDataTypeOption.SmallMoney: return typeof(decimal);
                case SqlDataTypeOption.Float: return typeof(double);
                case SqlDataTypeOption.Real: return typeof(float);
                case SqlDataTypeOption.UniqueIdentifier: return typeof(Guid);
                case SqlDataTypeOption.Int: return typeof(int);
                case SqlDataTypeOption.BigInt: return typeof(long);
                case SqlDataTypeOption.Sql_Variant: return typeof(object);
                case SqlDataTypeOption.SmallInt: return typeof(short);
                case SqlDataTypeOption.VarChar: return typeof(string);
                case SqlDataTypeOption.Text: return typeof(string);
                case SqlDataTypeOption.NChar: return typeof(string);
                case SqlDataTypeOption.NVarChar: return typeof(string);
                case SqlDataTypeOption.NText: return typeof(string);
                case SqlDataTypeOption.Image: return typeof(string);
                case SqlDataTypeOption.Time: return typeof(TimeSpan);
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
    }
}
