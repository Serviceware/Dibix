using System;
using System.Data;
using Dapper;

namespace Dibix.Dapper
{
    internal class DapperUriTypeHandler : SqlMapper.TypeHandler<Uri>
    {
        public override Uri Parse(object value) => new Uri((string)value, UriKind.RelativeOrAbsolute);

        public override void SetValue(IDbDataParameter parameter, Uri value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString();
        }
    }
}