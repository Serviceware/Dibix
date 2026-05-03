using System.CommandLine;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ArtifactCommand : Command
    {
        public ArtifactCommand() : base("artifact", "Manage Dibix artifacts such as NuGet packages and Docker images.")
        {
            EnvironmentVariableOption consumerDirectoryOption = new EnvironmentVariableOption("--consumer-directory", "c", EnvironmentVariableName.ConsumerDirectory, "The repository root of the Dibix consumer.");

            Add(consumerDirectoryOption);
            Add(new ClearNuGetPackagesCommand());
            Add(new ResetNuGetPackagesCommand(consumerDirectoryOption));
        }
    }
}