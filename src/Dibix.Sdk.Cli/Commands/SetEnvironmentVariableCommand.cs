using System;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal abstract class SetEnvironmentVariableCommand : ValidatableActionCommand
    {
        private readonly Argument<string> _valueArgument;

        protected abstract string EnvironmentVariableName { get; }
        protected virtual bool IsPath => false;

        public SetEnvironmentVariableCommand(string name, string commandDescription, string valueDescription, bool isSecret) : base(name, commandDescription)
        {
            _valueArgument = new Argument<string>("value") { Description = valueDescription };

            if (isSecret)
                _valueArgument.Arity = ArgumentArity.ZeroOrOne;

            Add(_valueArgument);
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            EnvironmentVariableTarget target = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process;
            string directory = parseResult.GetValue(_valueArgument) ?? ReadMaskedInput("Enter a value: ");
            if (IsPath && !Path.IsPathRooted(directory))
                directory = Path.GetFullPath(directory);

            Environment.SetEnvironmentVariable(EnvironmentVariableName, directory, target);
            return Task.FromResult(0);
        }

        private static string ReadMaskedInput(string prompt)
        {
            Console.Write(prompt);
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
            return sb.ToString();
        }
    }
}