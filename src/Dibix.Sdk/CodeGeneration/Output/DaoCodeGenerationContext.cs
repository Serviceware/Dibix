using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoCodeGenerationContext
    {
        private readonly CSharpRoot _root;

        public CSharpStatementScope Output { get; internal set; }
        public string GeneratedCodeAnnotation { get; }
        public CodeGenerationModel Model { get; }
        public bool WriteGuardChecks { get; set; }
        public bool GeneratePublicArtifacts => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;
        public bool WriteNamespaces => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;

        internal DaoCodeGenerationContext(CSharpRoot root, string generatedCodeAnnotation, CodeGenerationModel model)
        {
            this._root = root;
            this.Output = root;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.Model = model;
        }

        public DaoCodeGenerationContext AddUsing(string @using)
        {
            this._root.AddUsing(@using);
            return this;
        }
    }
}