using System.CommandLine;

namespace Dibix.Sdk
{
    public sealed class SetDebugVariableCommand : Command
    {
        public SetDebugVariableCommand() : base("set", "Set configuration values for debugging Dibix hosts.")
        {
            Add(new SetHttpHostDirectoryCommand());
            Add(new SetWorkerHostDirectoryCommand());
        }
    }
}