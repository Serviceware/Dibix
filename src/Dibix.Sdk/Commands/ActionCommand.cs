using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk
{
    public abstract class ActionCommand : Command
    {
        protected ActionCommand(string name, string description = null) : base(name, description)
        {
            SetAction(Execute);
            Validators.Add(Validate);
        }

        protected virtual void Validate(CommandResult result) { }

        protected abstract Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken);

        protected static void WriteLineInformation(string message) => Console.WriteLine(message);
        protected static void WriteLineDebug(string message) => WriteLine(message, ConsoleColor.DarkGray);
        protected static void WriteLineWarning(string message) => WriteLine(message, ConsoleColor.Yellow);
        protected static void WriteLineSuccess(string message) => WriteLine(message, ConsoleColor.Green);

        private static void WriteLine(string message, ConsoleColor foregroundColor)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}