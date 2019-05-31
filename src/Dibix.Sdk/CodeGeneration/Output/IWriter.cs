namespace Dibix.Sdk.CodeGeneration
{
    public interface IWriter
    {
        string Write(bool generatePublicArtifacts, string @namespace, string className, CommandTextFormatting formatting, SourceArtifacts artifacts);
    }
}