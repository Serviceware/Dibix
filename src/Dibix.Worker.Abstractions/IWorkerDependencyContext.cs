using System;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerDependencyContext
    {
        DbConnection Connection { get; }
        IDatabaseAccessorFactory DatabaseAccessorFactory { get; }
        string InitiatorFullName { get; }

        T GetExtension<T>() where T : notnull;
        T GetExtension<T>(Type implementationType) where T : notnull;
        ILogger CreateLogger(Type loggerType);
    }
}