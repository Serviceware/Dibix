namespace Dibix.Worker.Abstractions
{
    public interface IWorkerHostExtension
    {
        void Register(IWorkerHostExtensionConfigurationBuilder builder);
    }
}