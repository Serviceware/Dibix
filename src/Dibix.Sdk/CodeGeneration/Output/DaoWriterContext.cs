using System;
using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.Ast;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoWriterContext
    {
        private readonly Func<string, CommandTextFormatting, string> _commandTextFormatter;

        public CSharpRoot Output { get; }
        public string ClassName { get; }
        public CommandTextFormatting Formatting { get; }
        public SourceArtifacts Artifacts { get; }
        public string GeneratedCodeAnnotation { get; }
        public bool GeneratePublicArtifacts { get; }
        public bool WriteGuardChecks { get; set; }

        internal DaoWriterContext
        (
              CSharpRoot output
            , string generatedCodeAnnotation
            , bool generatePublicArtifacts
            , string className
            , CommandTextFormatting formatting
            , SourceArtifacts artifacts
            , Func<string, CommandTextFormatting, string> commandTextFormatter)
        {
            this._commandTextFormatter = commandTextFormatter;
            this.Output = output;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.GeneratePublicArtifacts = generatePublicArtifacts;
            this.ClassName = className;
            this.Formatting = formatting;
            this.Artifacts = artifacts;
        }

        public string FormatCommandText(string commandText, CommandTextFormatting formatting) => this._commandTextFormatter(commandText, formatting);
    }
}