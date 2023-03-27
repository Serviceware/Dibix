using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Dibix.Hosting.Abstractions.Data
{
    internal sealed class DefaultDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IOptionsMonitor<DatabaseOptions> _configuration;

        public DefaultDatabaseConnectionFactory(IOptionsMonitor<DatabaseOptions> configuration)
        {
            _configuration = configuration;
        }

        public DbConnection Create()
        {
            DbConnection connection = new SqlConnection(_configuration.CurrentValue.ConnectionString);
            connection.Open();
            return connection;
        }
    }
}