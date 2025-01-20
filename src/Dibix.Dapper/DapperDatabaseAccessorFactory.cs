using System.Data.Common;

namespace Dibix.Dapper
{
    public class DapperDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly DbProviderFactory _dbProviderFactory;
        private readonly string _connectionString;

        public DapperDatabaseAccessorFactory(DbProviderFactory dbProviderFactory, string connectionString)
        {
            _dbProviderFactory = dbProviderFactory;
            _connectionString = connectionString;
        }

        public IDatabaseAccessor Create()
        {
            DbConnection dbConnection = _dbProviderFactory.CreateConnection();
            dbConnection.ConnectionString = _connectionString;
            return new DapperDatabaseAccessor(dbConnection);
        }
    }
}