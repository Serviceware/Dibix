namespace Dibix.Sdk.Cli
{
    internal sealed class SetAlternateNuGetPackageFeedSourceCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.NuGetPackageAlternateFeedSource;

        public SetAlternateNuGetPackageFeedSourceCommand() : base("alternate-nuget-feed-source", "Set the alternate NuGet package source to push packages to.", "The package source (URL, UNC/folder path or package source name).")
        {
        }
    }
}