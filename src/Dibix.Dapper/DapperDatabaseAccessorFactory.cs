using System.Data.SqlClient;

namespace Dibix.Dapper
{
    public sealed class DapperDatabaseAccessorFactory : IDatabaseAccessorFactory
    {
        private readonly string _connectionString;

        public DapperDatabaseAccessorFactory(string connectionString) => this._connectionString = connectionString;

        public IDatabaseAccessor Create() => new DapperDatabaseAccessor(new SqlConnection(this._connectionString));
    }
}