using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Host
{
    internal sealed class EndpointMetadataMiddleware
    {
        private readonly RequestDelegate _next;

        public EndpointMetadataMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            EndpointDefinition? endpointDefinition = context.TryGetEndpointDefinition();
            if (endpointDefinition != null)
            {
                EndpointMetadataContext endpointMetadataContext = context.RequestServices.GetRequiredService<EndpointMetadataContext>();
                endpointMetadataContext.Initialize(endpointDefinition);
            }
            await _next(context).ConfigureAwait(false);
        }
    }
}