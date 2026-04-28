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
    }
}