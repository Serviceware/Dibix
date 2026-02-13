using System;

namespace Dibix
{
    public static class DatabaseAccessorFactoryExtensions
    {
        extension(IDatabaseAccessorFactory factory)
        {
            // Used by Dibix.Sdk
            public IDatabaseAccessor Create(string contextName, Action<DatabaseAccessorOptions> configure)
            {
                DibixTraceSource.Accessor.TraceInformation($"Executing database action '{contextName}'");
                IDatabaseAccessor accessor = Create(factory, configure);
                return accessor;
            }
            public IDatabaseAccessor Create(Action<DatabaseAccessorOptions> configure = null)
            {
                DatabaseAccessorOptions options = new DatabaseAccessorOptions();
                configure?.Invoke(options);
                IDatabaseAccessor accessor = factory.Create(options);
                return accessor;
            }
        }
    }
}