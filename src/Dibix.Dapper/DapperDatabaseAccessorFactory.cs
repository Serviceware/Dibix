using System.Data.Common;
using System.Data.SqlClient;

namespace Dibix.Dapper
{
    public class DapperDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly DbProviderFactory _dbProviderFactory;
        private readonly string _connectionString;

        public DapperDatabaseAccessorFactory(string connectionString) : this(SqlClientFactory.Instance, connectionString) { }
        public DapperDatabaseAccessorFactory(DbProviderFactory dbProviderFactory, string connectionString)
        {
            this._dbProviderFactory = dbProviderFactory;
            this._connectionString = connectionString;
        }

        public IDatabaseAccessor Create()
        {
            DbConnection dbConnection = this._dbProviderFactory.CreateConnection();
            dbConnection.ConnectionString = this._connectionString;
            return new DapperDatabaseAccessor(dbConnection);
        }
    }
}