using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Dibix.Http.Host
{
    internal sealed class McpEndpointRegistrar : IEndpointRegistrar
    {
        private readonly IEndpointMetadataProvider _endpointMetadataProvider;
        private readonly IEndpointImplementationProvider _endpointImplementationProvider;
        private readonly IServiceProviderIsService _serviceProviderIsService;
        private readonly IOptions<McpServerOptions> _options;
        private readonly ILogger<McpEndpointRegistrar> _logger;

        public McpEndpointRegistrar(IEndpointMetadataProvider endpointMetadataProvider, IEndpointImplementationProvider endpointImplementationProvider, IServiceProviderIsService serviceProviderIsService, IOptions<McpServerOptions> options, ILogger<McpEndpointRegistrar> logger)
        {
            _endpointMetadataProvider = endpointMetadataProvider;
            _endpointImplementationProvider = endpointImplementationProvider;
            _serviceProviderIsService = serviceProviderIsService;
            _options = options;
            _logger = logger;
        }

        public void Register(IEndpointRouteBuilder builder)
        {
            foreach (EndpointDefinition endpointDefinition in _endpointMetadataProvider.GetEndpoints())
            {
                CollectMcpPrimitive(endpointDefinition, endpointDefinition.ActionDefinition.ModelContextProtocolType, _options.Value);
            }
        }

        private void CollectMcpPrimitive(EndpointDefinition endpointDefinition, ModelContextProtocolType type, McpServerOptions mcpServerOptions)
        {
            switch (type)
            {
                case ModelContextProtocolType.None:
                    break;

                case ModelContextProtocolType.Tool:
                    CollectMcpTool(endpointDefinition, mcpServerOptions);
                    break;

                //case ModelContextProtocolType.Resource:
                //    break;
                //
                //case ModelContextProtocolType.Prompt:
                //    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void CollectMcpTool(EndpointDefinition endpointDefinition, McpServerOptions options)
        {
            _logger.LogDebug("Registering mcp server tool: {name}", endpointDefinition.ActionDefinition.ActionName);

            Delegate implementation = _endpointImplementationProvider.GetImplementation(endpointDefinition);
            AIFunctionFactoryOptions functionOptions = new AIFunctionFactoryOptions
            {
                Name = endpointDefinition.ActionDefinition.ActionName,
                Description = endpointDefinition.ActionDefinition.Description,
                MarshalResult = static (result, _, _) => new ValueTask<object?>(result),
                SerializerOptions = McpJsonUtilities.DefaultOptions,
                ConfigureParameterBinding = ConfigureParameterBinding
            };
            AIFunction function = AIFunctionFactory.Create(implementation.Method, implementation.Target, functionOptions);
            AIFunctionWrapper wrappedFunction = new AIFunctionWrapper(function, endpointDefinition);
            McpServerTool tool = McpServerTool.Create(wrappedFunction);

            options.Capabilities ??= new ServerCapabilities();
            options.Capabilities.Tools ??= new ToolsCapability();
            options.Capabilities.Tools.ToolCollection =
            [
                tool
            ];
        }

        private AIFunctionFactoryOptions.ParameterBindingOptions ConfigureParameterBinding(ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(HttpContext))
            {
                return new AIFunctionFactoryOptions.ParameterBindingOptions
                {
                    ExcludeFromSchema = true,
                    BindParameter = BindHttpContextParameter
                };
            }

            if (parameter.ParameterType == typeof(IHttpActionDelegator))
            {
                return new AIFunctionFactoryOptions.ParameterBindingOptions
                {
                    ExcludeFromSchema = true,
                    BindParameter = BindActionDelegatorParameter
                };
            }

            if (_serviceProviderIsService.IsService(parameter.ParameterType))
            {
                return new AIFunctionFactoryOptions.ParameterBindingOptions
                {
                    ExcludeFromSchema = true,
                    BindParameter = BindParameterFromService
                };
            }

            return default;
        }

        private static object? BindHttpContextParameter(ParameterInfo parameter, AIFunctionArguments arguments)
        {
            if (arguments.Context == null)
                throw CreateException(parameter, $"{typeof(AIFunctionArguments)}.{nameof(AIFunctionArguments.Context)} is null");

            if (arguments.Services == null)
                throw CreateException(parameter, $"{typeof(AIFunctionArguments)}.{nameof(arguments.Services)} is null");

            IHttpContextAccessor httpContextAccessor = arguments.Services.GetRequiredService<IHttpContextAccessor>();
            HttpContext? httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null)
                throw CreateException(parameter, "HttpContext is null");

            Endpoint? endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
                throw CreateException(parameter, "Endpoint is null");

            string endpointDefinitionKey = typeof(EndpointDefinition).ToString();
            if (arguments.Context[endpointDefinitionKey] is not EndpointDefinition endpointDefinition)
                throw CreateException(parameter, $"{nameof(arguments)}[{endpointDefinitionKey}] is null");

            EndpointMetadataCollection newEndpointMetadata = new EndpointMetadataCollection([.. endpoint.Metadata, endpointDefinition]);
            Endpoint newEndpoint = new Endpoint(endpoint.RequestDelegate, newEndpointMetadata, endpoint.DisplayName);
            httpContext.SetEndpoint(newEndpoint);

            return httpContext;
        }

        private static object? BindActionDelegatorParameter(ParameterInfo parameter, AIFunctionArguments arguments)
        {
            if (arguments.Services == null)
                throw new InvalidOperationException($"{typeof(AIFunctionArguments)}.{nameof(arguments.Services)} is null");

            McpResponseDelegator responseDelegator = ActivatorUtilities.CreateInstance<McpResponseDelegator>(arguments.Services, arguments);
            return responseDelegator;
        }

        private static object? BindParameterFromService(ParameterInfo parameter, AIFunctionArguments arguments)
        {
            if (arguments.Services == null)
                throw new InvalidOperationException($"{typeof(AIFunctionArguments)}.{nameof(arguments.Services)} is null");

            object? service = arguments.Services.GetService(parameter.ParameterType);
            if (service != null)
                return service;

            if (!parameter.HasDefaultValue)
                throw new ArgumentException("No service of the requested type was found.");

            return null;
        }

        private static InvalidOperationException CreateException(ParameterInfo parameterInfo, string message) => new InvalidOperationException($"Could not resolve value for parameter '{parameterInfo}': {message}");

        private sealed class AIFunctionWrapper : DelegatingAIFunction
        {
            private readonly EndpointDefinition _endpoint;

            public AIFunctionWrapper(AIFunction innerFunction, EndpointDefinition endpoint) : base(innerFunction)
            {
                _endpoint = endpoint;
            }
            protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
            {
                if (arguments.Services == null)
                    throw new InvalidOperationException($"{typeof(AIFunctionArguments)}.{nameof(arguments.Services)} is null");

                IHttpContextAccessor httpContextAccessor = arguments.Services.GetRequiredService<IHttpContextAccessor>();
                HttpContext? httpContext = httpContextAccessor.HttpContext;

                if (httpContext == null)
                    throw new InvalidOperationException("HttpContext is null");

                arguments.Context ??= new Dictionary<object, object?>();
                arguments.Context[typeof(EndpointDefinition).ToString()] = _endpoint;

                _ = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

                object? result = arguments[McpResponseDelegator.ResultKey];
                return result;
            }
        }

        private sealed class McpResponseDelegator : HttpActionInvokerBase, IHttpActionDelegator, IHttpResponseFormatter<HttpRequestDescriptor>
        {
            private readonly IControllerActivator _controllerActivator;
            private readonly IParameterDependencyResolver _parameterDependencyResolver;
            private readonly AIFunctionArguments _arguments;

            public const string ResultKey = $"{nameof(McpResponseDelegator)}.Result";

            public McpResponseDelegator(IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, AIFunctionArguments arguments)
            {
                _controllerActivator = controllerActivator;
                _parameterDependencyResolver = parameterDependencyResolver;
                _arguments = arguments;
            }

            public async Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments, CancellationToken cancellationToken)
            {
                EndpointDefinition endpointDefinition = httpContext.GetEndpointDefinition();
                HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
                object result = await Invoke(actionDefinition, new HttpRequestDescriptor(httpContext.Request), this, arguments, _controllerActivator, _parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                _arguments.Add(ResultKey, result);
            }

            public Task<object> Format(object result, HttpRequestDescriptor request, HttpActionDefinition action, CancellationToken cancellationToken)
            {
                return Task.FromResult(result);
            }
        }
    }
}