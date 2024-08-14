using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Host
{
    internal sealed class DatabaseScopeMiddleware
    {
        private readonly RequestDelegate _next;

        public DatabaseScopeMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            EndpointDefinition? endpointDefinition = context.TryGetEndpointDefinition();

            // Other APIs, i.E. /configuration
            if (endpointDefinition != null)
            {
                DatabaseScope databaseScope = context.RequestServices.GetRequiredService<DatabaseScope>();
                HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
                databaseScope.InitiatorFullName = actionDefinition.FullName;
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}