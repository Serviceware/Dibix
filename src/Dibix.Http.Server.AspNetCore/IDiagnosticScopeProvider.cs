using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server.AspNetCore
{
    public interface IDiagnosticScopeProvider
    {
        IReadOnlyCollection<KeyValuePair<string, object>> CollectScopeProperties(HttpContext context);
    }
}