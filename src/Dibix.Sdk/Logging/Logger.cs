using System;
using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk
{
    public class Logger : ILogger
    {
        private readonly TextWriter _output;
        private readonly ISet<string> _loggedErrors;

        public bool HasLoggedErrors { get; private set; }

        public Logger(TextWriter output, bool distinctErrorLogging)
        {
            _output = output;
            if (distinctErrorLogging)
                _loggedErrors = new HashSet<string>();
        }

        public void LogMessage(string text) => LogMessageCore(text);
        public virtual void LogMessage(LogCategory category, string subCategory, string code, string text, string source, int? line, int? column)
        {
            string categoryStr = GetCanonicalLogCategory(category);
            string message = CanonicalLogFormat.ToString(categoryStr, subCategory, code, text, source, line, column, endLine: null, endColumn: null);
            
            if (category == LogCategory.Error)
            {
                HasLoggedErrors = true;
                LogErrorMessage(message);
            }
            else
            {
                LogMessageCore(message);
            }
        }

        protected virtual void LogErrorMessage(string message)
        {
            bool shouldLogMessage = _loggedErrors == null || _loggedErrors.Add(message);
            if (shouldLogMessage)
                LogMessageCore(message);
        }

        private void LogMessageCore(string message) => _output.WriteLine(message);

        private static string GetCanonicalLogCategory(LogCategory category) => category switch
        {
            LogCategory.Warning => "warning",
            LogCategory.Error => "error",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}