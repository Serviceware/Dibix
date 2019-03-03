namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CodeGeneratorContext
    {
        private readonly ReportError _errorReporter;

        public string SourceFilePath { get; }
        public string Namespace { get; }

        public CodeGeneratorContext(string sourceFilePath, string @namespace, ReportError errorReporter)
        {
            this._errorReporter = errorReporter;
            this.SourceFilePath = sourceFilePath;
            this.Namespace = @namespace;
        }

        public void ReportError(uint level, string message, uint line, uint column) => this._errorReporter(level, message, line, column);
    }

    public delegate void ReportError(uint level, string message, uint line, uint column);
}