using System;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class WriterContext
    {
        private readonly CSharpRoot _root;
        private readonly Func<string, CommandTextFormatting, string> _commandTextFormatter;

        public CSharpStatementScope Output { get; internal set; }
        public string GeneratedCodeAnnotation { get; }
        public OutputConfiguration Configuration { get; }
        public SourceArtifacts Artifacts { get; }
        public bool WriteGuardChecks { get; set; }

        internal WriterContext
        (
              CSharpRoot root
            , string generatedCodeAnnotation
            , OutputConfiguration configuration
            , SourceArtifacts artifacts
            , Func<string, CommandTextFormatting, string> commandTextFormatter
        )
        {
            this._root = root;
            this._commandTextFormatter = commandTextFormatter;
            this.Output = root;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.Configuration = configuration;
            this.Artifacts = artifacts;
        }

        public string FormatCommandText(string commandText, CommandTextFormatting formatting) => this._commandTextFormatter(commandText, formatting);

        public WriterContext AddUsing(string @using)
        {
            this._root.AddUsing(@using);
            return this;
        }
    }
}