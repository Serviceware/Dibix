namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class SetHttpHostDirectoryCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Tools.EnvironmentVariableName.HttpHostDirectory;

        public SetHttpHostDirectoryCommand() : base("http-host-directory", "Set the host directory to mount Extension and Packages folders from when debugging Dibix http host.", "The directory of the http host to mount Extension and Packages folders from.")
        {
        }
    }
}