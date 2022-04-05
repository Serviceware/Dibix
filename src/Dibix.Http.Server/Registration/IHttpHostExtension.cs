namespace Dibix.Http.Server
{
    public interface IHttpHostExtension
    {
        void Register(IHttpHostExtensionConfigurationBuilder builder);
    }
}