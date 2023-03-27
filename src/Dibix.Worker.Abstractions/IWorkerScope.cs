using System;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerScope : IWorkerDependencyContext, IDisposable
    {
    }
}