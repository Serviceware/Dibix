#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace Dibix
{
    public sealed class MicrosoftSqlClientAdapter : SqlClientAdapter
    {
        private readonly SqlConnection? _sqlConnection;

        public override bool IsSqlClient => _sqlConnection != null;

        public MicrosoftSqlClientAdapter(DbConnection connection)
        {
            if (connection is not SqlConnection sqlConnection)
                return;

            _sqlConnection = sqlConnection;
            sqlConnection.InfoMessage += OnInfoMessage;
        }

        public override void DetachInfoMessageHandler()
        {
            if (_sqlConnection != null)
                _sqlConnection.InfoMessage -= OnInfoMessage;
        }

        public override int? TryGetSqlExceptionNumber(Exception exception) => exception is SqlException sqlException ? sqlException.Number : null;

        protected override object GetStructuredTypeParameterValue(StructuredType type)
        {
            SqlDataRecord[] records = MapRecords(type).ToArray();
            return records;
        }

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName)
        {
            if (parameter is not SqlParameter sqlParam)
                return;

            sqlParam.SqlDbType = sqlDbType;
            sqlParam.TypeName = typeName;
        }

        private static IEnumerable<SqlDataRecord> MapRecords(StructuredType type)
        {
            SqlMetaData[] metadata = MapMetadata(type).ToArray();

            foreach (Microsoft.SqlServer.Server.SqlDataRecord oldRecord in type.GetRecords())
            {
                SqlDataRecord newRecord = new SqlDataRecord(metadata);
                for (int i = 0; i < oldRecord.FieldCount; i++)
                    newRecord.SetValue(i, oldRecord.GetValue(i));

                yield return newRecord;
            }
        }

        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e) => OnInfoMessage(e.Message);

        private static IEnumerable<SqlMetaData> MapMetadata(StructuredType type)
        {
            foreach (Microsoft.SqlServer.Server.SqlMetaData oldMetadata in type.GetMetadata())
            {
                yield return new SqlMetaData
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
#nullable restore