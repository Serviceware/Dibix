using System;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class FileEntityTypeMap : DecoratedTypeMap, SqlMapper.ITypeMap
    {
        public static readonly Type Type = typeof(FileEntity);

        internal FileEntityTypeMap() : base(SqlMapper.GetTypeMap(Type), Type) { }

        public override SqlMapper.IMemberMap GetMember(string columnName)
        {
            // Dapper currently doesn't support reading individual columns as a stream instead of buffering them into memory.
            // See: https://github.com/DapperLib/Dapper/issues/893
            // Therefore we skip the data property during the initial read/parse and read it manually using DbDataReader.GetStream().
            if (String.Equals(columnName, nameof(FileEntity.Data), StringComparison.OrdinalIgnoreCase))
                return null;

            return base.GetMember(columnName);
        }
    }
}