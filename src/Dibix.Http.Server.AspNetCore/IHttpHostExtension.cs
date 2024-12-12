namespace Dibix.Http.Server.AspNetCore
{
    public interface IHttpHostExtension
    {
        void Register(IHttpHostExtensionConfigurationBuilder builder);
    }
}