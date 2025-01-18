using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace Dibix
{
    public abstract class SqlClientAdapter
    {
        private Action<string> _onInfoMessage;

        public abstract bool IsSqlClient { get; }

        public void AttachInfoMessageHandler(Action<string> handler)
        {
            _onInfoMessage = handler;
        }

        public abstract void DetachInfoMessageHandler();

        public abstract int? TryGetSqlExceptionNumber(Exception exception);

        public void MapStructuredTypeToParameter(IDbDataParameter parameter, StructuredType type)
        {
            IReadOnlyCollection<SqlDataRecord> records = type.GetRecords();
            parameter.Value = records.Any() ? GetStructuredTypeParameterValue(type) : null;
            SetProviderSpecificParameterProperties(parameter, SqlDbType.Structured, type.TypeName);
        }

        protected abstract object GetStructuredTypeParameterValue(StructuredType type);

        protected abstract void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName);

        protected void OnInfoMessage(string message) => _onInfoMessage?.Invoke(message);
    }
}