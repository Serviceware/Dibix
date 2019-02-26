namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlAccessorGeneratorConfigurationFactory
    {
        SqlAccessorGeneratorConfiguration CreateConfiguration(IExecutionEnvironment environment);
        PhysicalSourceConfiguration CreatePhysicalSourceConfiguration(IExecutionEnvironment environment, string projectName);
        DacPacSourceConfiguration CreateDacPacSourceConfiguration(IExecutionEnvironment environment, string packagePath);
    }
}