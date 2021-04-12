using System.Runtime.CompilerServices;

namespace Dibix.Testing
{
    public sealed class DatabaseConfiguration : LazyConfiguration
    {
        private string _connectionString;

        public string ConnectionString
        {
            get => base.GetProperty(ref this._connectionString);
            private set => this._connectionString = value;
        }

        public DatabaseConfiguration([CallerMemberName] string propertyName = null) : base(propertyName) { }
    }
}