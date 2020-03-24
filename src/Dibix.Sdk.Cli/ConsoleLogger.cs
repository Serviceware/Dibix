using System;

namespace Dibix.Sdk.Cli
{
    internal sealed class ConsoleLogger : ILogger
    {
        public bool HasLoggedErrors { get; private set; }

        public void LogMessage(string text) => Console.WriteLine(text);
        public void LogError(string code, string text, string source, int line, int column)
        {
            Console.WriteLine(CanonicalLogFormat.ToErrorString(code, text, source, line, column));
            this.HasLoggedErrors = true;
        }
    }
}