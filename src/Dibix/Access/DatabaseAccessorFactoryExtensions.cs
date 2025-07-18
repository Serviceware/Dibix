namespace Dibix
{
    public static class DatabaseAccessorFactoryExtensions
    {
        public static IDatabaseAccessor Create(this IDatabaseAccessorFactory factory, string contextName)
        {
            DibixTraceSource.Accessor.TraceInformation($"Executing database action '{contextName}'");
            IDatabaseAccessor accessor = factory.Create();
            return accessor;
        }
    }
}