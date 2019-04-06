using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoWriterContext
    {
        private readonly Func<string, CommandTextFormatting, string> _commandTextFormatter;

        public CSharpWriter Output { get; }
        public string ClassName { get; }
        public CommandTextFormatting Formatting { get; }
        public IList<SqlStatementInfo> Statements { get; }
        public string GeneratedCodeAnnotation { get; }
        public bool WriteGuardChecks { get; set; }

        internal DaoWriterContext
        (
            CSharpWriter output
          , string generatedCodeAnnotation
          , string className
          , CommandTextFormatting formatting
          , IList<SqlStatementInfo> statements
          , Func<string, CommandTextFormatting, string> commandTextFormatter)
        {
            this._commandTextFormatter = commandTextFormatter;
            this.Output = output;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.ClassName = className;
            this.Formatting = formatting;
            this.Statements = statements;
        }

        public string FormatCommandText(string commandText, CommandTextFormatting formatting) => this._commandTextFormatter(commandText, formatting);
    }
}