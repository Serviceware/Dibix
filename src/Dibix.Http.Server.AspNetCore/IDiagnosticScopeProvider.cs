using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server.AspNetCore
{
    public interface IDiagnosticScopeProvider
    {
        IEnumerable<KeyValuePair<string, object>> CollectScopeProperties(HttpContext context);
    }
}