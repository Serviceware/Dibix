using System;

namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionConfigurationBuilder
    {
        IHttpHostExtensionConfigurationBuilder EnableRequestIdentityProvider();
        IHttpHostExtensionConfigurationBuilder OverrideAuthenticationHandler<T>();
        IHttpHostExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string, string> configure);
    }
}