using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using Dibix.Http.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddBuiltinHttpMessageHandlers(this IHttpClientBuilder builder, params Type[] excludedHandlers)
        {
            AddHttpMessageHandler<TraceProxyHttpMessageHandler>(builder, excludedHandlers);
            AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>(builder, excludedHandlers);
            AddHttpMessageHandler<TraceSourceHttpMessageHandler>(builder, excludedHandlers);
            return builder;
        }

        public static void AddDibixHttpClient(this IHttpClientBuilder httpClientBuilder, Action<IHttpServiceDiscoveryConfiguration> configure)
        {
            HttpServiceConfiguration configuration = new HttpServiceConfiguration(httpClientBuilder);
            configure(configuration);

            IServiceCollection services = httpClientBuilder.Services;
            services.Configure(configuration.ClientConfiguration ?? (_ => { }));

            RegisterHttpClients(services, configuration.AssemblyCollector, httpClientBuilder.Name);
        }

        private static void AddHttpMessageHandler<T>(IHttpClientBuilder builder, IEnumerable<Type> excludedHandlers) where T : DelegatingHandler, new()
        {
            if (excludedHandlers.Contains(typeof(T)))
                return;

            builder.AddHttpMessageHandler(() => new T());
        }

        private static void RegisterHttpClients(IServiceCollection services, Func<ICollection<Assembly>> assemblyCollector, string httpClientName)
        {
            foreach (Assembly assembly in assemblyCollector())
            {
                if (!assembly.IsDefined(typeof(ArtifactAssemblyAttribute)))
                    continue;

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass)
                        continue;

                    HttpServiceAttribute httpServiceAttribute = type.GetCustomAttribute<HttpServiceAttribute>();
                    if (httpServiceAttribute == null)
                        continue;

                    services.AddScoped(httpServiceAttribute.ContractType, CompileImplementationFactory(type, httpClientName));
                }
            }
        }

        private static Func<IServiceProvider, object> CompileImplementationFactory(Type implementationType, string httpClientName)
        {
            MethodCallExpression ResolveService(Type contractType, Expression serviceProvider) => Expression.Call(typeof(ServiceProviderServiceExtensions), nameof(ServiceProviderServiceExtensions.GetRequiredService), [contractType], serviceProvider);

            Expression CollectParameter(string parameterName, Type parameterType, Expression serviceProvider) => parameterName switch
            {
                "httpClientOptions" when parameterType == typeof(HttpClientOptions) => Expression.Property(ResolveService(typeof(IOptions<HttpClientOptions>), serviceProvider), nameof(IOptions<object>.Value)),
                "httpClientName" when parameterType == typeof(string) => Expression.Constant(httpClientName),
                _ => ResolveService(parameterType, serviceProvider)
            };

            ParameterExpression serviceProviderParameter = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");

            ConstructorInfo constructor = HttpServiceConstructorSelector.SelectConstructor(implementationType);
            IEnumerable<Expression> parameters = constructor.GetParameters().Select(x => CollectParameter(x.Name!, x.ParameterType, serviceProviderParameter));
            Expression value = Expression.New(constructor, parameters);

            Expression<Func<IServiceProvider, object>> lambda = Expression.Lambda<Func<IServiceProvider, object>>(value, serviceProviderParameter);
            Func<IServiceProvider, object> compiled = lambda.Compile();
            return compiled;
        }

        private sealed class HttpServiceConfiguration : IHttpServiceDiscoveryConfiguration, IHttpServiceInfrastructureConfiguration
        {
            private readonly IHttpClientBuilder _httpClientBuilder;

            public Func<ICollection<Assembly>> AssemblyCollector { get; private set; } = () => [];
            public Action<HttpClientOptions> ClientConfiguration { get; set; }

            public HttpServiceConfiguration(IHttpClientBuilder httpClientBuilder)
            {
                _httpClientBuilder = httpClientBuilder;
            }

            IHttpServiceInfrastructureConfiguration IHttpServiceDiscoveryConfiguration.FromAssemblies(IEnumerable<Assembly> assemblies)
            {
                AssemblyCollector = () => HttpServiceAssemblyDiscovery.FromAssemblies(assemblies);
                return this;
            }

            IHttpServiceInfrastructureConfiguration IHttpServiceDiscoveryConfiguration.FromAssembly(Assembly assembly)
            {
                AssemblyCollector = () => HttpServiceAssemblyDiscovery.FromAssembly(assembly);
                return this;
            }

            IHttpServiceInfrastructureConfiguration IHttpServiceDiscoveryConfiguration.FromAssemblyContaining(Type type)
            {
                AssemblyCollector = () => HttpServiceAssemblyDiscovery.FromAssemblyContaining(type);
                return this;
            }

            IHttpServiceInfrastructureConfiguration IHttpServiceInfrastructureConfiguration.WithAuthorizationProvider<TAuthorizationProvider>()
            {
                _httpClientBuilder.Services.TryAddScoped<IHttpAuthorizationProvider, TAuthorizationProvider>();
                return this;
            }

            void IHttpServiceInfrastructureConfiguration.Configure(Action<HttpClientOptions> configure)
            {
                ClientConfiguration = configure;
            }
        }

        private static class HttpServiceAssemblyDiscovery
        {
            public static ICollection<Assembly> FromAssemblies(IEnumerable<Assembly> assemblies) => assemblies.ToArray();

            public static ICollection<Assembly> FromAssembly(Assembly assembly) => [assembly];

            public static ICollection<Assembly> FromAssemblyContaining(Type type) => FromAssembly(type.Assembly);
        }
    }
}