using System;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionConfigurationBuilder
    {
        IHttpHostExtensionConfigurationBuilder EnableRequestIdentityProvider();
#if NET
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<T>(string schemeName) where T : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>;
#endif
        IHttpHostExtensionConfigurationBuilder RegisterClaimsTransformer<T>() where T : class, IClaimsTransformer;
        IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string, string> configure);
        IHttpHostExtensionConfigurationBuilder OnHostStarted(Func<IHttpHostExtensionScope, Task> handler);
        IHttpHostExtensionConfigurationBuilder OnHostStopped(Func<IHttpHostExtensionScope, Task> handler);
    }
}