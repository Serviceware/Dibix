using System.CommandLine;

namespace Dibix.Sdk.Cli
{
    internal sealed class SetConfigurationVariableCommand : Command
    {
        public SetConfigurationVariableCommand() : base("set", "Set configuration value defaults for more convenient CLI command use without passing options.")
        {
            Add(new SetHttpHostDirectoryCommand());
            Add(new SetWorkerHostDirectoryCommand());
            Add(new SetConsumerDirectoryCommand());
            Add(new SetAlternateNuGetPackageFeedSourceCommand());
            Add(new SetAlternateNuGetPackageFeedApiKeyCommand());
            Add(new SetOfficialNuGetPackageFeedApiKeyCommand());
            Add(new SetDockerHubUserNameCommand());
            Add(new SetDockerHubPasswordCommand());
        }
    }
}