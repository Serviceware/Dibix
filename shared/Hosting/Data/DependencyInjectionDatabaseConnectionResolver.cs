using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Hosting.Abstractions.Data
{
    internal sealed class DependencyInjectionDatabaseConnectionResolver : IDatabaseConnectionResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionDatabaseConnectionResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DbConnection Resolve() => _serviceProvider.GetRequiredService<DbConnection>();
    }
}