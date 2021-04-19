namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerationModel : CodeGenerationModel
    {
        public string ProductName { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string DefaultOutputFilePath { get; set; }
        public string ClientOutputFilePath { get; set; }

        public CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel compatibilityLevel) : base(compatibilityLevel)
        {
            base.CommandTextFormatting = CommandTextFormatting.SingleLine;
        }
    }
}