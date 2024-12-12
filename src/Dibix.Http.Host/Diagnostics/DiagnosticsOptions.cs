using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host
{
    internal sealed class DiagnosticsOptions
    {
        public IDiagnosticScopeProvider Provider { get; set; } = new DefaultDiagnosticScope();
    }
}