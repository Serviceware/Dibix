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
            this._errorOutput = new StringBuilder();
        }

        public override void LogError(string subCategory, string code, string text, string source, int? line, int? column)
        {
            string relativeSource = source.Substring(DatabaseTestUtility.DatabaseProjectDirectory.Length + 1);
            base.LogError(subCategory, code, text, relativeSource, line, column);
        }

        protected override void LogError(string text)
        {
            base.LogError(text);
            this._errorOutput.AppendLine(text);
        }

        public void Verify()
        {
            if (this.HasLoggedErrors)
                throw new CodeGenerationException(this._errorOutput.ToString());
        }
    }
}