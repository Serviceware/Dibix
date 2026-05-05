using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal abstract class ValidatableActionCommand : Command
    {
        protected ValidatableActionCommand(string name, string description = null) : base(name, description)
        {
            SetAction(ExecuteCore);
            Validators.Add(ValidateCore);
        }

        protected virtual void Validate(CommandResult commandResult) { }

        protected abstract Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken);

        private async Task<int> ExecuteCore(ParseResult parseResult, CancellationToken cancellationToken)
        {
            try
            {
                return await Execute(parseResult, cancellationToken).ConfigureAwait(false);
            }
            catch (ProcessExecutionException exception)
            {
                ConsoleUtility.WriteLineError(exception.Message);
                return 1;
            }
        }

        private void ValidateCore(CommandResult commandResult)
        {
            try
            {
                Validate(commandResult);
            }
            catch (CommandLineValidationException exception)
            {
                commandResult.AddError(exception.Message);
            }
        }
    }
}