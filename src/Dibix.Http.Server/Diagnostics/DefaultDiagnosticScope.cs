using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server
{
    public class DefaultDiagnosticScope : IDiagnosticScopeProvider
    {
        protected const string UnavailableValue = "<Unavailable>";

        IEnumerable<KeyValuePair<string, object>> IDiagnosticScopeProvider.CollectScopeProperties(HttpContext context)
        {
            return CollectScopeProperties(context).Where(x => x != null)
                                                  .Select(x => x.Value)
                                                  .Select(x => new KeyValuePair<string, object>(x.Key, x.Value));
        }

        protected virtual IEnumerable<(string Key, object Value)?> CollectScopeProperties(HttpContext context)
        {
            yield return RequestMethod(context);
            yield return User(context);
            yield return Action(context);
        }

        protected virtual (string Key, string Value)? RequestMethod(HttpContext context) => ("RequestMethod", context.Request.Method);

        protected virtual (string, string)? User(HttpContext context) => ("User", context.User?.Identity?.Name ?? UnavailableValue);

        protected virtual (string, string)? Action(HttpContext context) => ("Action", context.TryGetEndpointDefinition()?.ActionDefinition.ActionName ?? UnavailableValue);
    }
}