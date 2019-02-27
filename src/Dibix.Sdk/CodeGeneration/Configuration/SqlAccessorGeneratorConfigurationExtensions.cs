namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlAccessorGeneratorConfigurationExtensions
    {
        public static void ApplyFromJson(this SqlAccessorGeneratorConfiguration configuration, string json, IExecutionEnvironment environment, ISqlAccessorGeneratorConfigurationFactory configurationFactory)
        {
            ISqlAccessorGeneratorConfigurationReader reader = new JsonSqlAccessorGeneratorConfigurationReader(environment, configurationFactory, json);
            reader.Read(configuration);
        }
    }
}