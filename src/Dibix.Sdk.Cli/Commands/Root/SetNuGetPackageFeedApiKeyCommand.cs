namespace Dibix.Sdk.Cli
{
    internal sealed class SetNuGetPackageFeedApiKeyCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.NuGetPackageFeedApiKey;

        public SetNuGetPackageFeedApiKeyCommand() : base("package-feed-api-key", "Set the API key to be used when pushing to NuGet package feed.", "The API key.")
        {
        }
    }
}