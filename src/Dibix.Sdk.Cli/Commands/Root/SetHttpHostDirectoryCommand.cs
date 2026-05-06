namespace Dibix.Sdk.Cli
{
    internal sealed class SetHttpHostDirectoryCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.HttpHostDirectory;
        protected override bool IsPath => true;

        public SetHttpHostDirectoryCommand() : base("http-host-directory", "Set the host directory to mount Extension and Packages folders from when debugging Dibix http host.", "The directory of the http host to mount Extension and Packages folders from.")
        {
        }
    }
}