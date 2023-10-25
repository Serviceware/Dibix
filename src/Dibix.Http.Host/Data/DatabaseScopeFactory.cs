using Dibix.Http.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Host
{
    internal sealed class DatabaseScopeFactory : IDatabaseScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IDatabaseAccessorFactory Create<TInitiator>() => Create(typeof(TInitiator).FullName!);
        public IDatabaseAccessorFactory Create(string initiatorFullName)
        {
            IServiceScope serviceScope = _scopeFactory.CreateScope();
            DatabaseScope databaseScope = serviceScope.ServiceProvider.GetRequiredService<DatabaseScope>();
            databaseScope.InitiatorFullName = initiatorFullName;
            IDatabaseAccessorFactory databaseAccessorFactory = serviceScope.ServiceProvider.GetRequiredService<IDatabaseAccessorFactory>();
            return databaseAccessorFactory;
        }
    }
}