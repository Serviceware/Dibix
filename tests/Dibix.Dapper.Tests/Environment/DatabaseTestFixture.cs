using System;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Dibix.Dapper.Tests
{
    public sealed class DatabaseTestFixture
    {
        private readonly Func<DbConnection> _connectionFactory;

        public DatabaseTestFixture()
        {
            /* 
                In .NET Framework, the providers are automatically available via machine.config and are also registered globally in the GAC.
                In .NET Core, there is no GAC or global configuration anymore.
                This means we have to register the provider first 
            */
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            // Load configuration and setup connection factory
            ConnectionStringSection connectionSection = LoadConfiguration();
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory(connectionSection.ProviderName);
            this._connectionFactory = () =>
            {
                DbConnection connection = providerFactory.CreateConnection();
                connection.ConnectionString = connectionSection.ConnectionString;
                return connection;
            };
        }

        public IDatabaseAccessor CreateDatabaseAccessor() => new DapperDatabaseAccessor(this._connectionFactory());

        private static ConnectionStringSection LoadConfiguration()
        {
            ConnectionStringOptions connectionSections = new ConfigurationBuilder().AddUserSecrets("dibix")
                                                                                   .AddEnvironmentVariables()
                                                                                   .Build()
                                                                                   .Get<ConnectionStringOptions>();
            Guard.IsNotNull(connectionSections, nameof(connectionSections), "No connection string configured. Please call setup-env script.");
            ConnectionStringSection connectionSection = connectionSections["DefaultConnection"];
            return connectionSection;
        }
    }
}