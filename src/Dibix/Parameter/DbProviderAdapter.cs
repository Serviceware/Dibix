using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Dibix
{
    public abstract class DbProviderAdapter<TConnection> : DbProviderAdapter where TConnection : DbConnection
    {
        public TConnection Connection { get; }

        protected DbProviderAdapter(TConnection connection)
        {
            Connection = connection;
        }
    }

    public abstract class DbProviderAdapter
    {
        private Action<string> _onInfoMessage;
        
        public abstract bool UsesTSql { get; }

        public void AttachInfoMessageHandler(Action<string> handler)
        {
            _onInfoMessage = handler;
            AttachInfoMessageHandler();
        }

        public abstract void DetachInfoMessageHandler();

        public abstract int? TryGetSqlErrorNumber(Exception exception);

        public void MapStructuredTypeToParameter(IDbDataParameter parameter, StructuredType type)
        {
            bool hasRecords = type.GetRecords().Any();
            parameter.Value = hasRecords ? GetStructuredTypeParameterValue(type) : null;
            SetProviderSpecificParameterProperties(parameter, SqlDbType.Structured, type.TypeName);
        }

        protected abstract object GetStructuredTypeParameterValue(StructuredType type);

        protected abstract void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName);

        protected abstract void AttachInfoMessageHandler();

        protected void OnInfoMessage(string message) => _onInfoMessage?.Invoke(message);
    }
}