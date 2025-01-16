using System.Data;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperStructuredTypeParameter : SqlMapper.ICustomQueryParameter
    {
        #region Fields
        private readonly StructuredType _udt;
        private readonly SqlDataRecordAdapter _sqlDataRecordAdapter;
        #endregion

        #region Constructor
        public DapperStructuredTypeParameter(StructuredType udt, SqlDataRecordAdapter sqlDataRecordAdapter)
        {
            _udt = udt;
            _sqlDataRecordAdapter = sqlDataRecordAdapter;
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
            _sqlDataRecordAdapter.MapStructuredTypeToParameter(param, _udt);

            return param;
        }
        #endregion
    }
}