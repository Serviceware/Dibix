namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlAccessorGeneratorConfigurationExtensions
    {
        public static void ApplyFromJson(this SqlAccessorGeneratorConfiguration configuration, string json, IExecutionEnvironment environment)
        {
            ISqlAccessorGeneratorConfigurationReader reader = new JsonSqlAccessorGeneratorConfigurationReader(environment, json);
            reader.Read(configuration);
        }
    }
}