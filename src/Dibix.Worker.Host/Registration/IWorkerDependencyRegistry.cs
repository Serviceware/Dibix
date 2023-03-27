using System;

namespace Dibix.Worker.Host
{
    internal interface IWorkerDependencyRegistry
    {
        bool IsRegistered(Type type);
    }
}