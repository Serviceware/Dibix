namespace Dibix.Sdk.CodeGeneration
{
    public interface IDaoWriter
    {
        string RegionName { get; }

        bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts);
        void Write(DaoWriterContext context);
    }
}