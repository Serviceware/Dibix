using System;
using System.Data;

namespace Dibix
{
    public sealed class DefaultDbProviderAdapter : DbProviderAdapter
    {
        public static DbProviderAdapter Instance => new DefaultDbProviderAdapter();

        public override bool UsesTSql => false;

        public override void DetachInfoMessageHandler() { }

        public override int? TryGetSqlErrorNumber(Exception exception) => null;

        protected override void AttachInfoMessageHandler() { }

        protected override object GetStructuredTypeParameterValue(StructuredType type) => throw new NotSupportedException();

        protected override void SetProviderSpecificParameterProperties(IDbDataParameter parameter, SqlDbType sqlDbType, string typeName) => throw new NotSupportedException();
    }
}