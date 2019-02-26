namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlAccessorGeneratorConfigurationFactory : ISqlAccessorGeneratorConfigurationFactory
    {
        #region ISqlAccessorGeneratorConfigurationFactory Members
        public SqlAccessorGeneratorConfiguration CreateConfiguration(IExecutionEnvironment environment) => new SqlAccessorGeneratorConfiguration
        {
            Output =
            {
                Writer = typeof(SqlDaoWriter),
                Namespace = environment.GetProjectDefaultNamespace(),
                ClassName = environment.GetClassName(),
                Formatting = SqlQueryOutputFormatting.Singleline
            }
        };

        public PhysicalSourceConfiguration CreatePhysicalSourceConfiguration(IExecutionEnvironment environment, string projectName)
        {
            PhysicalSourceConfiguration configuration = new PhysicalSourceConfiguration(environment, projectName);
            SetDefaultValues(configuration);
            return configuration;
        }

        public DacPacSourceConfiguration CreateDacPacSourceConfiguration(IExecutionEnvironment environment, string packagePath)
        {
            DacPacSourceConfiguration configuration = new DacPacSourceConfiguration(environment, packagePath);
            SetDefaultValues(configuration);
            return configuration;
        }
        #endregion

        #region Private Methods
        private static void SetDefaultValues(SourceConfiguration configuration)
        {
            configuration.Parser = typeof(SqlStoredProcedureParser);
            configuration.Formatter = typeof(TakeSourceSqlStatementFormatter);
        }
        #endregion
    }
}