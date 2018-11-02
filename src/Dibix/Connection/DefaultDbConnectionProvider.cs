using System;
using System.Data.Common;

namespace Dibix
{
    public class DefaultDbConnectionProvider : IDbConnectionProvider, IDisposable
    {
        #region Fields
        private readonly DbConnection _connection;
        #endregion

        #region Constructor
        public DefaultDbConnectionProvider(DbConnection connection)
        {
            this._connection = connection;

        }
        #endregion

        #region IDbConnectionProvider Members
        public DbConnection GetConnection() => this._connection;
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this._connection?.Dispose();
        }
        #endregion
    }
}