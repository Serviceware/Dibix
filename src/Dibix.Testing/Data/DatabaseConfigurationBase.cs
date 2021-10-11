namespace Dibix.Testing.Data
{
    public abstract class DatabaseConfigurationBase
    {
        public virtual DatabaseConfiguration Database { get; } = new DatabaseConfiguration();
    }
}