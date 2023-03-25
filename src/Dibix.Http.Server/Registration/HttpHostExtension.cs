namespace Dibix.Http.Server
{
    public abstract class HttpHostExtension
    {
        public abstract void Register(IHttpHostExtensionConfigurationBuilder builder);
#if NET

        public virtual void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) { }

        public virtual void ConfigureApplication(Microsoft.AspNetCore.Builder.WebApplication application) { }
#endif
    }
}