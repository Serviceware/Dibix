namespace Dibix.Sdk.Cli
{
    internal sealed class SetWorkerHostDirectoryCommand : SetEnvironmentVariableCommand
    {
        protected override string EnvironmentVariableName => Cli.EnvironmentVariableName.WorkerHostDirectory;
        protected override bool IsPath => true;

        public SetWorkerHostDirectoryCommand() : base("worker-host-directory", "Set the host directory to mount Extension and Workers folders from when debugging Dibix worker host.", "The directory of the worker host to mount Extension and Workers folders from.")
        {
        }
    }
}