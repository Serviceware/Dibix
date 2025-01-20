using System;
using System.Data;
#if NET
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Dibix
{
    public sealed class SqlClientAdapter : DbProviderAdapter<SqlConnection>
    {
        public override bool UsesTSql => true;

        public SqlClientAdapter(SqlConnection connection) : base(connection)
        {
            connection.InfoMessage += OnInfoMessage;
        }

        protected override void AttachInfoMessageHandler()
        {
            Connection.InfoMessage += OnInfoMessage;
        }

        public override void DetachInfoMessageHandler()
        {
            Connection.InfoMessage -= OnInfoMessage;
        }

        public override int? TryGetSqlErrorNumber(Exception exception) => exception is SqlException sqlException ? sqlException.Number : null;

        protected override object GetStructuredTypeParameterValue(StructuredType type) => type.GetRecords();

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName)
        {
            SqlParameter sqlParameter = (SqlParameter)parameter;
            sqlParameter.SqlDbType = sqlDbType;
            sqlParameter.TypeName = typeName;
        }

        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e) => OnInfoMessage(e.Message);
    }
}