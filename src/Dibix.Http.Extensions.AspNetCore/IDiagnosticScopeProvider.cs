using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server
{
    public interface IDiagnosticScopeProvider
    {
        IEnumerable<KeyValuePair<string, object>> CollectScopeProperties(HttpContext context);
    }
}