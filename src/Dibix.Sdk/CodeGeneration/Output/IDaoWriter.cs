namespace Dibix.Sdk.CodeGeneration
{
    public interface IDaoWriter
    {
        string RegionName { get; }

        bool HasContent(SourceArtifacts artifacts);
        void Write(DaoWriterContext context);
    }
}