using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;

namespace Dibix.Http.Host
{
    // VSCode or the Model Context Protocol does not support the .well-known endpoint to be behind a subpath
    // Error: Invalid discovery URL: expected path to start with /.well-known/oauth-protected-resource
    // See: https://github.com/microsoft/vscode/issues/256236
    // Therefore we skip prefixing the endpoint URL with the PathBase, but add it at the end of the path.
    // i.E. /WebSite/.well-known/oauth-protected-resource is changed to /.well-known/oauth-protected-resource/WebSite
    // This handler skips the PathBase prefix. The suffix is added via McpAuthenticationOptions.ResourceMetadataUri.
    internal sealed class NoPathBaseMcpAuthenticationHandler : McpAuthenticationHandler
    {
        public NoPathBaseMcpAuthenticationHandler(IOptionsMonitor<McpAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            PathString pathBase = Request.PathBase;
            Request.PathBase = null;
            try
            {
                await base.HandleChallengeAsync(properties).ConfigureAwait(false);
            }
            finally
            {
                Request.PathBase = pathBase;
            }
        }
    }
}