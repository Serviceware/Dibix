using System.Data;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperStructuredTypeParameter : SqlMapper.ICustomQueryParameter
    {
        #region Fields
        private readonly StructuredType _udt;
        private readonly SqlClientAdapter _sqlClientAdapter;
        #endregion

        #region Constructor
        public DapperStructuredTypeParameter(StructuredType udt, SqlClientAdapter sqlClientAdapter)
        {
            _udt = udt;
            _sqlClientAdapter = sqlClientAdapter;
        }
        #endregion

        #region ICustomQueryParameter Members
        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            Guard.IsNotNull(command, nameof(command));
            command.Parameters.Add(ToSqlParameter(command, name));
        }
        #endregion

        #region Private Methods
        private IDbDataParameter ToSqlParameter(IDbCommand command, string parameterName)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = parameterName;
            _sqlClientAdapter.MapStructuredTypeToParameter(param, _udt);

            return param;
        }
        #endregion
    }
}