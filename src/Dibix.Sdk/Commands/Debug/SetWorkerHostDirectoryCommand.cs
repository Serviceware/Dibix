namespace Dibix.Sdk
{
    public sealed class SetWorkerHostDirectoryCommand : SetHostDirectoryCommand
    {
        protected override string EnvironmentVariableName => Dibix.Sdk.EnvironmentVariableName.WorkerHostDirectory;

        public SetWorkerHostDirectoryCommand() : base("worker-host-directory", "Set the host directory to mount Extension and Workers folders from when debugging Dibix worker host.", "The directory of the worker host to mount Extension and Workers folders from.")
        {
        }
    }
}