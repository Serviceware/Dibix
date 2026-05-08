namespace Dibix.Sdk.Cli
{
    internal sealed class SetAlternateNuGetPackageFeedApiKeyCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.NuGetPackageAlternateFeedApiKey;

        public SetAlternateNuGetPackageFeedApiKeyCommand() : base("alternate-nuget-feed-api-key", "Set the API key to be used when pushing to the alternate NuGet package feed.", "The API key.", isSecret: true)
        {
        }
    }
}