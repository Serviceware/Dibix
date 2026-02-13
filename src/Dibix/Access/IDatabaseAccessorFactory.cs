namespace Dibix
{
    public interface IDatabaseAccessorFactory
    {
        IDatabaseAccessor Create(DatabaseAccessorOptions options);
    }
}