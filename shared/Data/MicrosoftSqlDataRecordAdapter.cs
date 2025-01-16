using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dibix
{
    public sealed class MicrosoftSqlDataRecordAdapter : SqlDataRecordAdapter
    {
        protected override object CollectValue(StructuredType type)
        {
            Microsoft.Data.SqlClient.Server.SqlDataRecord[] records = MapRecords(type).ToArray();
            return records;
        }

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName)
        {
            if (parameter is not Microsoft.Data.SqlClient.SqlParameter sqlParam)
                return;

            sqlParam.SqlDbType = sqlDbType;
            sqlParam.TypeName = typeName;
        }

        private static IEnumerable<Microsoft.Data.SqlClient.Server.SqlDataRecord> MapRecords(StructuredType type)
        {
            Microsoft.Data.SqlClient.Server.SqlMetaData[] metadata = MapMetadata(type).ToArray();

            foreach (Microsoft.SqlServer.Server.SqlDataRecord oldRecord in type.GetRecords())
            {
                Microsoft.Data.SqlClient.Server.SqlDataRecord newRecord = new Microsoft.Data.SqlClient.Server.SqlDataRecord(metadata);
                for (int i = 0; i < oldRecord.FieldCount; i++)
                    newRecord.SetValue(i, oldRecord.GetValue(i));

                yield return newRecord;
            }
        }

        private static IEnumerable<Microsoft.Data.SqlClient.Server.SqlMetaData> MapMetadata(StructuredType type)
        {
            foreach (Microsoft.SqlServer.Server.SqlMetaData oldMetadata in type.GetMetadata())
            {
                yield return new Microsoft.Data.SqlClient.Server.SqlMetaData
                (
                    name: oldMetadata.Name
                  , dbType: oldMetadata.SqlDbType
                  , maxLength: oldMetadata.MaxLength
                  , precision: oldMetadata.Precision
                  , scale: oldMetadata.Scale
                  , locale: default
                  , compareOptions: default
                  , userDefinedType: default
                );
            }
        }
    }
}