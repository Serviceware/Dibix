using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class MapConfigurationExtensions
    {
        public static IConfigurationMappingExpression<TOptions> ConfigureTarget<TOptions>(this IServiceCollection services, IConfiguration configuration) where TOptions : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddOptions();
            return ConfigurationMappingExpression<TOptions>.Create(name: Options.Options.DefaultName, services, configuration);
        }

        private sealed class ConfigurationMappingExpression<TToOptions> : IConfigurationMappingExpression<TToOptions> where TToOptions : class
        {
            private readonly IConfiguration _configuration;
            private readonly IDictionary<Func<IServiceProvider, object?>, Delegate> _mappings;

            public ConfigurationMappingExpression(IConfiguration configuration)
            {
                _configuration = configuration;
                _mappings = new Dictionary<Func<IServiceProvider, object?>, Delegate>();
            }

            public static IConfigurationMappingExpression<TToOptions> Create(string name, IServiceCollection services, IConfiguration configuration)
            {
                ConfigurationMappingExpression<TToOptions> expression = new ConfigurationMappingExpression<TToOptions>(configuration);
                services.AddSingleton<IOptionsChangeTokenSource<TToOptions>>(new ConfigurationChangeTokenSource<TToOptions>(name, configuration));
                services.AddSingleton<IConfigureOptions<TToOptions>>(x => new ConfigureNamedOptions<TToOptions>(name, y => expression.Map(x, y)));
                return expression;
            }

            public IConfigurationMappingExpression<TToOptions> MapFrom<TFromOptions>(string configurationName, Action<TFromOptions, TToOptions> mapper) where TFromOptions : new()
            {
                _mappings.Add(x => ResolveOptionsValue<TFromOptions>(x, configurationName), mapper);
                return this;
            }

            private TFromOptions ResolveOptionsValue<TFromOptions>(IServiceProvider serviceProvider, string configurationName) where TFromOptions : new()
            {
                // Not stable
                // When the event for IOptionsMonitor<TToOptions> fires, IOptionsMonitor<TFromOptions> might not be updated already
                //return serviceProvider.GetRequiredService<IOptionsMonitor<TFromOptions>>().CurrentValue;

                return _configuration.GetSection(configurationName).Bind<TFromOptions>();
            }

            private void Map(IServiceProvider serviceProvider, TToOptions to)
            {
                foreach (KeyValuePair<Func<IServiceProvider, object?>, Delegate> mapping in _mappings)
                {
                    object? from = mapping.Key.Invoke(serviceProvider);
                    mapping.Value.DynamicInvoke(from, to);
                }
            }
        }
    }

    internal interface IConfigurationMappingExpression<out TToOptions>
    {
        IConfigurationMappingExpression<TToOptions> MapFrom<TFromOptions>(string configurationName, Action<TFromOptions, TToOptions> mapper) where TFromOptions : new();
    }
}