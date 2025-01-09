using System.Collections.Generic;
using System.Threading.Tasks;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class DiagnosticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiagnosticsMiddleware> _logger;
        private readonly IOptions<DiagnosticsOptions> _diagnosticsOptions;

        public DiagnosticsMiddleware(RequestDelegate next, ILogger<DiagnosticsMiddleware> logger, IOptions<DiagnosticsOptions> diagnosticsOptions)
        {
            _next = next;
            _logger = logger;
            _diagnosticsOptions = diagnosticsOptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            IDiagnosticScopeProvider scopeProvider = _diagnosticsOptions.Value.Provider;
            IReadOnlyCollection<KeyValuePair<string, object>> scopeProperties = scopeProvider.CollectScopeProperties(context);
            using (_logger.BeginScope(scopeProperties))
            {
                await _next(context).ConfigureAwait(false);
            }
        }
    }
}