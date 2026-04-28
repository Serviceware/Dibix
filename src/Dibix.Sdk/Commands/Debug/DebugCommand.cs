using System.CommandLine;

namespace Dibix.Sdk
{
    public sealed class DebugCommand : Command
    {
        public DebugCommand() : base("debug", "Commands to assist in debugging Dibix hosts.")
        {
            Add(new ConfigureDebugCommand());
            Add(new SetDebugVariableCommand());
        }
    }
}