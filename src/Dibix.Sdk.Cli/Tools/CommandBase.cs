using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class EnvironmentVariableOption : Option<string>
    {
        private readonly string _environmentVariableName;

        public EnvironmentVariableOption(string optionName, string alias, string environmentVariableName, string description = null) : base(optionName, alias)
        {
            _environmentVariableName = environmentVariableName;
            Description = description;
        }

        public string CollectValue(CommandResult commandResult, bool isRequired, ref bool loggedMessages)
        {
            string targetDirectory = commandResult.GetValue(this);
            if (targetDirectory != null)
                return targetDirectory;

            // Enable configuring the environment variable via the Windows environment variables settings dialog, but on linux fallback to process, where registry access is not available
            EnvironmentVariableTarget target = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process;
            targetDirectory = Environment.GetEnvironmentVariable(_environmentVariableName, target);

            if (targetDirectory != null)
            {
                ConsoleUtility.WriteLineDebug($"Read {Name} option from {_environmentVariableName} environment variable because it wasn't passed to this command");
                loggedMessages = true;
            }
            else if (isRequired)
            {
                commandResult.AddError($"Error: Either the {Name} option or {_environmentVariableName} environment variable must be provided.");
            }

            return targetDirectory;
        }
    }
}