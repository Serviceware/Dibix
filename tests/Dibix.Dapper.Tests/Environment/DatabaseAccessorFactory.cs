using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Dibix.Dapper.Tests
{
    internal static class DatabaseAccessorFactory
    {
        public static IDatabaseAccessor Create()
        {
            ConnectionStringOptions connectionSections = new ConfigurationBuilder().AddUserSecrets("dibix")
                                                                                   .Build()
                                                                                   .Get<ConnectionStringOptions>();
            Guard.IsNotNull(connectionSections, nameof(connectionSections), "No connection string configured. Please call setup-env script.");
            ConnectionStringSection connectionSection = connectionSections["DefaultConnection"];

            /* 
                In .NET Framework, the providers are automatically available via machine.config and are also registered globally in the GAC.
                In .NET Core, there is no GAC or global configuration anymore.
                This means we have to register the provider first 
            */
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            DbConnection connection = DbProviderFactories.GetFactory(connectionSection.ProviderName)
                                                         .CreateConnection();
            connection.ConnectionString = connectionSection.ConnectionString;
            connection.Open();

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = @"IF TYPE_ID('[dbo].[_dibix_tests_structuredtype]') IS NULL
BEGIN
	CREATE TYPE [dbo].[_dibix_tests_structuredtype] AS TABLE
	(
		[intvalue]     INT			     NOT NULL
	  , [stringvalue]  NVARCHAR(MAX) NOT NULL
	  , [decimalvalue] DECIMAL(14,2) NOT NULL
	  , PRIMARY KEY ([intvalue])
	)
END

DROP PROCEDURE IF EXISTS [dbo].[_dibix_tests_sp1]
EXEC(N'CREATE PROCEDURE [dbo].[_dibix_tests_sp1] @out INT OUTPUT
AS
	SET @out = 5')";
                command.ExecuteNonQuery();
            }

            return new DapperDatabaseAccessor(connection);
        }
    }
}