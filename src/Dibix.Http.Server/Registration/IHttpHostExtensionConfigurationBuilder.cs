using System;

namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionConfigurationBuilder
    {
        IHttpHostExtensionConfigurationBuilder EnableRequestIdentityProvider();
#if NET
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<T>(string schemeName) where T : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>;
#endif
        IHttpHostExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface;
        IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string, string> configure);
    }
}