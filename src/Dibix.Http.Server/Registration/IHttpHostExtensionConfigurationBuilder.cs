using System;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionConfigurationBuilder
    {
#if NET
        Microsoft.Extensions.Configuration.IConfigurationRoot Configuration { get; }
#endif

        IHttpHostExtensionConfigurationBuilder EnableRequestIdentityProvider();
        IHttpHostExtensionConfigurationBuilder ConfigureOptions<TOptions>(string sectionName, Action<TOptions, string> optionsMonitorSubscriber = null) where TOptions : class;
#if NET
        IHttpHostExtensionConfigurationBuilder ConfigureOptions<TOptions>(Microsoft.Extensions.Configuration.IConfiguration configuration, Action<TOptions, string> optionsMonitorSubscriber = null) where TOptions : class;
        IHttpHostExtensionConfigurationBuilder ConfigureJwtBearer(Action<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> configure);
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<THandler, TOptions>(string schemeName) where THandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new();
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<THandler, TOptions>(string schemeName, Action<TOptions> configureOptions) where THandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TOptions> where TOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, new();
#endif
        IHttpHostExtensionConfigurationBuilder RegisterClaimsTransformer<T>() where T : class, IClaimsTransformer;
        IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string, string> configure);
#if NET
        IHttpHostExtensionConfigurationBuilder ConfigureDiagnosticScopeProvider<TProvider>() where TProvider : IDiagnosticScopeProvider, new();
#endif
        IHttpHostExtensionConfigurationBuilder OnHostStarted(Func<IHttpHostExtensionScope, Task> handler);
        IHttpHostExtensionConfigurationBuilder OnHostStopped(Func<IHttpHostExtensionScope, Task> handler);
    }
}