using System;
using Dibix.Http.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Host
{
    internal sealed class ParameterDependencyResolver : IParameterDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ParameterDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Resolve<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    }
}