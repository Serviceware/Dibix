using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Worker.Host
{
    internal sealed class ServiceScopeWorkerScopeFactory : IWorkerScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceScopeWorkerScopeFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IWorkerScope Create<TInitiator>() => Create(typeof(TInitiator).FullName!);
        IWorkerScope IWorkerScopeFactory.Create(string initiatorFullName) => Create(initiatorFullName);

        public WorkerScope Create(string initiatorFullName)
        {
            IServiceScope scope = _scopeFactory.CreateScope();
            ServiceProviderWorkerDependencyContext dependencyContext = scope.ServiceProvider.GetRequiredService<ServiceProviderWorkerDependencyContext>();
            dependencyContext.InitiatorFullName = initiatorFullName;
            return new WorkerScope(dependencyContext, scope.Dispose, initiatorFullName);
        }
    }
}