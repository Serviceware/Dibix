using System.Threading.Tasks;
using Dibix.Http.Host.Extensions;
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
            DatabaseScope databaseScope = context.RequestServices.GetRequiredService<DatabaseScope>();
            EndpointDefinition endpointDefinition = context.GetEndpointDefinition();
            HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
            databaseScope.InitiatorFullName = actionDefinition.Executor.Method.Name;

            await _next(context).ConfigureAwait(false);
        }
    }
}