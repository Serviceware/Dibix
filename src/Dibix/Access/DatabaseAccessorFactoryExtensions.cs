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
                DatabaseAccessorOptions options = PrepareOptions(configure);
                IDatabaseAccessor accessor = factory.Create(contextName, options);
                return accessor;
            }
            // Used by Dibix.Sdk
            public IDatabaseAccessor Create(string contextName, DatabaseAccessorOptions options)
            {
                DibixTraceSource.Accessor.TraceInformation($"Executing database action '{contextName}'");
                IDatabaseAccessor accessor = factory.Create(options);
                return accessor;
            }
            public IDatabaseAccessor Create(Action<DatabaseAccessorOptions> configure = null)
            {
                DatabaseAccessorOptions options = PrepareOptions(configure);
                IDatabaseAccessor accessor = factory.Create(options);
                return accessor;
            }
        }

        private static DatabaseAccessorOptions PrepareOptions(Action<DatabaseAccessorOptions> configure)
        {
            DatabaseAccessorOptions options = new DatabaseAccessorOptions();
            configure?.Invoke(options);
            options.BufferResult ??= false;
            return options;
        }
    }
}