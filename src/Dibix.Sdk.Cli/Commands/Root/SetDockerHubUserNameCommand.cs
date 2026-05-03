namespace Dibix.Sdk.Cli
{
    internal sealed class SetDockerHubUserNameCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.DockerHubUserName;

        public SetDockerHubUserNameCommand() : base("docker-hub-username", "Set the user name used to delete images on Docker Hub.", "The username to authenticate with Docker Hub.")
        {
        }
    }
}