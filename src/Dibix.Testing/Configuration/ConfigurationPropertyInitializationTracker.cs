using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Dibix.Testing
{
    public sealed class ConfigurationPropertyInitializationTracker
    {
        private readonly ICollection<string> _initializedProperties = new HashSet<string>();
        private string _path;

        internal ConfigurationInitializationToken InitializationToken { get; }

        public ConfigurationPropertyInitializationTracker(ConfigurationInitializationToken initializationToken) => InitializationToken = initializationToken;

        public void EnterSection(string path) => this._path = path;

        public void Verify([CallerMemberName] string propertyName = null)
        {
            if (!this.InitializationToken.IsInitialized)
                return;

            this.EnsurePath();
            if (!this._initializedProperties.Contains(propertyName))
                throw new InvalidOperationException($"Property not configured: {this.BuildPath(propertyName)}");
        }

        public void Initialize([CallerMemberName] string propertyName = null)
        {
            this.EnsurePath();
            this._initializedProperties.Add(propertyName);
        }

        private string BuildPath(string propertyName)
        {
            StringBuilder sb = new StringBuilder(this._path);
            if (sb.Length > 0)
                sb.Append(':');

            sb.Append(propertyName);
            return sb.ToString();
        }

        private void EnsurePath()
        {
            if (this._path == null)
                throw new InvalidOperationException("No configuration section has been initialized yet");
        }
    }
}