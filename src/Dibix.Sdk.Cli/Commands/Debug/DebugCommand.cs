using System.CommandLine;

namespace Dibix.Sdk.Cli
{
    internal sealed class DebugCommand : Command
    {
        public DebugCommand() : base("debug", "Setup local debugging for existing deployments of Dibix hosts.")
        {
            Add(new ConfigureDebugCommand());
        }
    }
}