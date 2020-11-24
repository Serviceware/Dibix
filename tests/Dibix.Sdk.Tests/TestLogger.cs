using System.Text;
using Dibix.Sdk.Tests.CodeGeneration;
using Xunit.Abstractions;

namespace Dibix.Sdk.Tests
{
    internal class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _messageOutput;
        private readonly StringBuilder _errorOutput;

        public bool HasLoggedErrors => this._errorOutput.Length > 0;

        public TestLogger(ITestOutputHelper messageOutput)
        {
            this._messageOutput = messageOutput;
            this._errorOutput = new StringBuilder();
        }

        public void LogMessage(string text) => this._messageOutput.WriteLine(text);

        public void LogError(string code, string text, string source, int? line, int? column) => this.LogError(subCategory: null, code: code, text: text, source: source, line: line, column: column);
        public void LogError(string subCategory, string code, string text, string source, int? line, int? column)
        {
            string relativeSource = source.Substring(DatabaseTestUtility.DatabaseProjectDirectory.Length + 1);
            this._errorOutput.AppendLine(CanonicalLogFormat.ToErrorString(subCategory, code, text, relativeSource, line, column));
        }

        public void Verify()
        {
            if (this.HasLoggedErrors)
                throw new CodeGenerationException(this._errorOutput.ToString());
        }
    }
}