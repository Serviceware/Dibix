using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Dibix
{
    public sealed class SystemSqlClientAdapter : SqlClientAdapter
    {
        private readonly SqlConnection _sqlConnection;

        public override bool IsSqlClient => _sqlConnection != null;

        public SystemSqlClientAdapter(DbConnection connection)
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

        protected override object GetStructuredTypeParameterValue(StructuredType type) => type.GetRecords();

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName)
        {
            if (parameter is not SqlParameter sqlParam)
                return;

            sqlParam.SqlDbType = sqlDbType;
            sqlParam.TypeName = typeName;
        }

        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e) => OnInfoMessage(e.Message);
    }
}