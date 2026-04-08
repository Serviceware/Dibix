namespace Dibix.Testing.Data
{
    public partial class DatabaseConfiguration
    {
        [LazyValidation]
        private string _connectionString;
    }
}