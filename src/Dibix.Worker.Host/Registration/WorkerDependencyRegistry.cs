using System;
using System.Collections.Generic;

namespace Dibix.Worker.Host
{
    internal sealed class WorkerDependencyRegistry : IWorkerDependencyRegistry
    {
        private readonly ICollection<Type> _dependencyRegistrations = new HashSet<Type>();

        public bool IsRegistered(Type type) => _dependencyRegistrations.Contains(type);

        public void Register(Type type) => _dependencyRegistrations.Add(type);
    }
}