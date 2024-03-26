using System.Diagnostics;
using System.IO;
using System.Text;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Tests.CodeGeneration;

namespace Dibix.Sdk.Tests
{
    internal class TestLogger : Logger, ILogger
    {
        private readonly StringBuilder _errorOutput;

        public TestLogger(TextWriter output, bool distinctErrorLogging) : base(output, distinctErrorLogging)
        {
            _errorOutput = new StringBuilder();
        }

        public override void LogMessage(LogCategory category, string subCategory, string code, string text, string source, int? line, int? column)
        {
            string relativeSource = source.Substring(DatabaseTestUtility.DatabaseProjectDirectory.Length + 1);
            base.LogMessage(category, subCategory, code, text, relativeSource, line, column);
        }

        protected override void LogErrorMessage(string text)
        {
            base.LogErrorMessage(text);
            _errorOutput.AppendLine(text);
            Debug.WriteLine(text);
        }

        public void Verify()
        {
            if (HasLoggedErrors)
            {
                string errorMessages = _errorOutput.ToString();
                throw new CodeGenerationException(errorMessages);
            }
        }
    }
}