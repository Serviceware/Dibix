using System.Data;
using System.Data.SqlClient;

namespace Dibix
{
    public sealed class SystemSqlClientDataRecordAdapter : SqlDataRecordAdapter
    {
        protected override object CollectValue(StructuredType type) => type.GetRecords();

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName)
        {
            if (parameter is not SqlParameter sqlParam)
                return;

            sqlParam.SqlDbType = sqlDbType;
            sqlParam.TypeName = typeName;
        }
    }
}