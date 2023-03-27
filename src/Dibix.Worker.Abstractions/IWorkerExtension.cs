namespace Dibix.Worker.Abstractions
{
    public interface IWorkerExtension
    {
        void Register(IWorkerExtensionConfigurationBuilder builder);
    }
}