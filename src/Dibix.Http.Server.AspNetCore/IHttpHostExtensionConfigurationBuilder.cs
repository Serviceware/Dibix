using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace Dibix.Http.Server.AspNetCore
{
    public interface IHttpHostExtensionConfigurationBuilder
    {
        IConfigurationRoot Configuration { get; }

        IHttpHostExtensionConfigurationBuilder ConfigureOptions<TOptions>(string sectionName, Action<TOptions, string> optionsMonitorSubscriber = null) where TOptions : class;
        IHttpHostExtensionConfigurationBuilder ConfigureOptions<TOptions>(IConfiguration configuration, Action<TOptions, string> optionsMonitorSubscriber = null) where TOptions : class;
        IHttpHostExtensionConfigurationBuilder ConfigureJwtBearer(Action<JwtBearerOptions> configure);
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<THandler, TOptions>(string schemeName) where THandler : AuthenticationHandler<TOptions> where TOptions : AuthenticationSchemeOptions, new();
        IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<THandler, TOptions>(string schemeName, Action<TOptions> configureOptions) where THandler : AuthenticationHandler<TOptions> where TOptions : AuthenticationSchemeOptions, new();
        IHttpHostExtensionConfigurationBuilder RegisterClaimsTransformer<T>() where T : class, IClaimsTransformer;
        IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string, string> configure);
        IHttpHostExtensionConfigurationBuilder ConfigureDiagnosticScopeProvider<TProvider>() where TProvider : IDiagnosticScopeProvider, new();
        IHttpHostExtensionConfigurationBuilder OnHostStarted(Func<IHttpHostExtensionScope, Task> handler);
        IHttpHostExtensionConfigurationBuilder OnHostStopped(Func<IHttpHostExtensionScope, Task> handler);
    }
}