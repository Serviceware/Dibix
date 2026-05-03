namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class SetConsumerDirectoryCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Tools.EnvironmentVariableName.ConsumerDirectory;

        public SetConsumerDirectoryCommand() : base("consumer-directory", "Set the directory of a Dibix consumer to manage Dibix package versions.", "The repository root of the Dibix consumer.")
        {
        }
    }
}