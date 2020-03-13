using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.SqlServer.Server;

namespace Dibix.Dapper
{
    internal sealed class DapperStructuredTypeParameter : SqlMapper.ICustomQueryParameter
    {
        #region Fields
        private readonly Func<IEnumerable<SqlDataRecord>> _recordsProvider;
        #endregion

        #region Properties
        public string UdtName { get; }
        #endregion

        #region Constructor
        public DapperStructuredTypeParameter(string udtName, Func<IEnumerable<SqlDataRecord>> recordsProvider)
        {
            this.UdtName = udtName;
            this._recordsProvider = recordsProvider;
        }
        #endregion

        #region ICustomQueryParameter Members
        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            Guard.IsNotNull(command, nameof(command));
            command.Parameters.Add(this.ToSqlParameter(command, name));
        }
        #endregion

        #region Private Methods
        private IDbDataParameter ToSqlParameter(IDbCommand command, string parameterName)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = parameterName;
            param.Value = this.GetValue();

            if (param is SqlParameter sqlParam)
            {
                sqlParam.SqlDbType = SqlDbType.Structured;
                sqlParam.TypeName = this.UdtName;
            }

            return param;
        }

        private object GetValue()
        {
            ICollection<SqlDataRecord> records = this.GetRecords() as ICollection<SqlDataRecord> ?? this.GetRecords().ToArray();
            return records.Any() ? records : null;
        }

        private IEnumerable<SqlDataRecord> GetRecords()
        {
            return this._recordsProvider();
        }
        #endregion
    }
}