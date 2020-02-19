using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class CodeArtifactsGenerationModel : CodeGenerationModel
    {
        public string ProductName { get; set; }
        public string DefaultOutputFilePath { get; set; }
        public string ClientOutputFilePath { get; set; }

        public CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel compatibilityLevel) : base(compatibilityLevel)
        {
            base.CommandTextFormatting = CommandTextFormatting.Singleline;
        }
    }
}