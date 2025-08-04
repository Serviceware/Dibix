using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace Dibix.Http.Host
{
    internal sealed class EndpointMetadataMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _mcpPath;

        public EndpointMetadataMiddleware(RequestDelegate next, string mcpPath)
        {
            _next = next;
            _mcpPath = mcpPath;
        }

        public async Task Invoke(HttpContext context)
        {
            Endpoint? endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                // Collect from regular Dibix endpoint
                EndpointDefinition? endpointDefinition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
                if (endpointDefinition != null)
                {
                    EndpointMetadataContext endpointMetadataContext = context.RequestServices.GetRequiredService<EndpointMetadataContext>();
                    endpointMetadataContext.Initialize(endpointDefinition.ActionDefinition.ActionName, endpointDefinition.ActionDefinition.ValidAudiences);
                }

                // Collect from MCP server tool call
                if (context.Request.Path.StartsWithSegments(_mcpPath) && context.Request.Method == HttpMethods.Post)
                {
                    AcceptsMetadata? acceptsMetadata = endpoint.Metadata.GetMetadata<AcceptsMetadata>();
                    if (acceptsMetadata is { IsOptional: false } && acceptsMetadata.ContentTypes.Contains("application/json"))
                    {
                        // TODO TODO_MCP: Body is read twice => Bad
                        context.Request.EnableBuffering();

                        JsonRpcMessage? message = await JsonSerializer.DeserializeAsync<JsonRpcMessage>(context.Request.Body).ConfigureAwait(false);
                        context.Request.Body.Position = 0;

                        if (message is JsonRpcRequest { Method: RequestMethods.ToolsCall } jsonRpcRequest)
                        {
                            CallToolRequestParams? parameters = jsonRpcRequest.Params.Deserialize<CallToolRequestParams>();
                            if (parameters != null)
                            {
                                EndpointMetadataContext endpointMetadataContext = context.RequestServices.GetRequiredService<EndpointMetadataContext>();
                                endpointMetadataContext.Initialize(parameters.Name, validAudiences: []);
                            }
                        }
                    }
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}