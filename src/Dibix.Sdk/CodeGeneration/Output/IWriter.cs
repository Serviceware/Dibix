namespace Dibix.Sdk.CodeGeneration
{
    public interface IWriter
    {
        string Write(OutputConfiguration configuration, SourceArtifacts artifacts);
    }
}