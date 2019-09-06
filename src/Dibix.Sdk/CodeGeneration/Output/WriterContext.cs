using System;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class WriterContext
    {
        private readonly Func<string, CommandTextFormatting, string> _commandTextFormatter;

        public CSharpRoot Output { get; }
        public string GeneratedCodeAnnotation { get; }
        public OutputConfiguration Configuration { get; }
        public SourceArtifacts Artifacts { get; }
        public bool WriteGuardChecks { get; set; }

        internal WriterContext
        (
              CSharpRoot output
            , string generatedCodeAnnotation
            , OutputConfiguration configuration
            , SourceArtifacts artifacts
            , Func<string, CommandTextFormatting, string> commandTextFormatter)
        {
            this._commandTextFormatter = commandTextFormatter;
            this.Output = output;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.Configuration = configuration;
            this.Artifacts = artifacts;
        }

        public string FormatCommandText(string commandText, CommandTextFormatting formatting) => this._commandTextFormatter(commandText, formatting);
    }
}