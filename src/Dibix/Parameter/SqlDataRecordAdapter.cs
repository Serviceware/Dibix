using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace Dibix
{
    public abstract class SqlDataRecordAdapter
    {
        public void MapStructuredTypeToParameter(IDbDataParameter parameter, StructuredType type)
        {
            IReadOnlyCollection<SqlDataRecord> records = type.GetRecords();
            parameter.Value = records.Any() ? CollectValue(type) : null;
            SetProviderSpecificParameterProperties(parameter, SqlDbType.Structured, type.TypeName);
        }

        protected abstract object CollectValue(StructuredType type);

        protected abstract void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName);
    }
}