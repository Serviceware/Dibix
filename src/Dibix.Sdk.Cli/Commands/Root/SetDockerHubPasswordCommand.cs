namespace Dibix.Sdk.Cli
{
    internal sealed class SetDockerHubPasswordCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.DockerHubPassword;

        public SetDockerHubPasswordCommand() : base("docker-hub-password", "Set the password used to delete images on Docker Hub.", "The password to authenticate with Docker Hub.")
        {
        }
    }
}