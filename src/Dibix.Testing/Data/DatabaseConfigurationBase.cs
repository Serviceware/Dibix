using Dibix.Testing.Generators;

namespace Dibix.Testing.Data
{
    public abstract partial class DatabaseConfigurationBase
    {
        [LazyValidation]
        private DatabaseConfiguration _database;
    }
}