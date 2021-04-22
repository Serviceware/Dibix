namespace Dibix.Testing.Data
{
    public abstract class DatabaseConfigurationBase : LazyConfiguration
    {
        public DatabaseConfiguration Database { get; } = new DatabaseConfiguration();
    }
}