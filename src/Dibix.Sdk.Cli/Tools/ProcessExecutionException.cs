using System;
using System.Text;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ProcessExecutionException : Exception
    {
        public ProcessExecutionException(string fileName, string arguments, int exitCode, string standardOutput, string standardError) : base(CreateMessage(fileName, arguments, exitCode, standardOutput, standardError))
        {
        }

        private static string CreateMessage(string fileName, string arguments, int exitCode, string standardOutput, string standardError)
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(standardOutput))
            {
                sb.AppendLine(standardOutput);
            }

            if (!String.IsNullOrWhiteSpace(standardError))
            {
                sb.AppendLine(standardError);
            }

            sb.AppendLine()
              .AppendLine($"'{fileName} {arguments}' exited with code {exitCode}.");

            string message = sb.ToString();
            return message;
        }
    }
}