namespace Dibix.Testing
{
    public abstract class DatabaseConfigurationBase : LazyConfiguration
    {
        public DatabaseConfiguration Database { get; } = new DatabaseConfiguration();
    }
}