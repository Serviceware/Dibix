using System;
using System.Data;
using Dapper;

namespace Dibix.Dapper
{
    public static class DapperExtensions
    {
        internal static object AsDapperParams(this IParametersVisitor parametersVisitor)
        {
            Guard.IsNotNull(parametersVisitor, nameof(parametersVisitor));
            DynamicParameters @params = new DynamicParameters();
            parametersVisitor.VisitParameters((name, value, type, suggestedDataType) => @params.Add(name, NormalizeParameterValue(value), NormalizeParameterDbType(suggestedDataType)));
            return @params;
        }

        private static object NormalizeParameterValue(object value)
        {
            const string schemaName = "dbo";
            if (value is StructuredType tvp)
                return new DapperStructuredTypeParameter(schemaName, tvp.TypeName, tvp.GetRecords);

            return value;
        }

        private static DbType? NormalizeParameterDbType(DbType? dbType)
        {
            if (dbType == DbType.Xml)
                return null; // You would guess DbType.Xml, but since Dapper treats .NET XML types (i.E. XElement) as custom types, DbType = null is expected

            return dbType;
        }
    }
}