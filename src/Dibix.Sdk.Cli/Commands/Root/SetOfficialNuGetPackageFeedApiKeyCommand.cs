namespace Dibix.Sdk.Cli
{
    internal sealed class SetOfficialNuGetPackageFeedApiKeyCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.NuGetPackageOfficialFeedApiKey;

        public SetOfficialNuGetPackageFeedApiKeyCommand() : base("official-nuget-feed-api-key", "Set the API key to be used when unlisting packages from the official NuGet package feed.", "The API key.")
        {
        }
    }
}