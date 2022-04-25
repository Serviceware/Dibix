using System.Collections.Generic;
using System.IO;

namespace Dibix.Sdk
{
    public class Logger : ILogger
    {
        private readonly TextWriter _output;
        private readonly ISet<string> _loggedErrors;

        public bool HasLoggedErrors { get; private set; }

        public Logger(TextWriter output, bool distinctErrorLogging)
        {
            this._output = output;
            if (distinctErrorLogging)
                this._loggedErrors = new HashSet<string>();
        }

        public void LogMessage(string text) => this._output.WriteLine(text);

        public void LogError(string code, string text, string source, int? line, int? column) => this.LogError(subCategory: null, code: code, text: text, source: source, line: line, column: column);
        public virtual void LogError(string subCategory, string code, string text, string source, int? line, int? column)
        {
            string message = CanonicalLogFormat.ToErrorString(subCategory, code, text, source, line, column);
            if (this._loggedErrors == null || this._loggedErrors.Add(message))
                this.LogError(message);

            this.HasLoggedErrors = true;
        }

        protected virtual void LogError(string text) => this._output.WriteLine(text);
    }
}