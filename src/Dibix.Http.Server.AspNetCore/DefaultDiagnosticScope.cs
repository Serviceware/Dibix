using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server.AspNetCore
{
    public class DefaultDiagnosticScope : IDiagnosticScopeProvider
    {
        protected const string UnavailableValue = "<Unavailable>";

        IReadOnlyCollection<KeyValuePair<string, object>> IDiagnosticScopeProvider.CollectScopeProperties(HttpContext context)
        {
            return CollectScopeProperties(context).Where(x => x != null)
                                                  .Select(x => x.Value)
                                                  .Select(x => new KeyValuePair<string, object>(x.Key, x.Value))
                                                  .ToArray();
        }

        protected virtual IEnumerable<(string Key, object Value)?> CollectScopeProperties(HttpContext context)
        {
            yield return ThreadId();
            yield return RequestMethod(context);
            yield return User(context);
            yield return Action(context);
        }

        private static (string Key, string Value)? ThreadId() => ("ThreadId", Environment.CurrentManagedThreadId.ToString());

        protected virtual (string Key, string Value)? RequestMethod(HttpContext context) => ("RequestMethod", context.Request.Method);

        protected virtual (string Key, string Value)? User(HttpContext context) => ("User", context.User?.Identity?.Name ?? UnavailableValue);

        protected virtual (string Key, string Value)? Action(HttpContext context) => ("Action", context.TryGetEndpointDefinition()?.ActionDefinition.ActionName ?? UnavailableValue);
    }
}