using System;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal abstract class SetEnvironmentVariableCommand : ValidatableActionCommand
    {
        private readonly Argument<string> _valueArgument;

        protected abstract string EnvironmentVariableName { get; }
        protected virtual bool IsPath => false;

        public SetEnvironmentVariableCommand(string name, string commandDescription, string valueDescription) : base(name, commandDescription)
        {
            _valueArgument = new Argument<string>("value") { Description = valueDescription };

            Add(_valueArgument);
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            EnvironmentVariableTarget target = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process;
            string directory = parseResult.GetValue(_valueArgument);
            if (IsPath && !Path.IsPathRooted(directory))
                directory = Path.GetFullPath(directory);

            Environment.SetEnvironmentVariable(EnvironmentVariableName, directory, target);
            return Task.FromResult(0);
        }
    }
}