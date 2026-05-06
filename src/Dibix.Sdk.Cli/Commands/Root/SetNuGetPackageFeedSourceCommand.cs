namespace Dibix.Sdk.Cli
{
    internal sealed class SetNuGetPackageFeedSourceCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.NuGetPackageFeedSource;

        public SetNuGetPackageFeedSourceCommand() : base("package-feed-source", "Set the NuGet package source to push packages to.", "The package source (URL, UNC/folder path or package source name).")
        {
        }
    }
}