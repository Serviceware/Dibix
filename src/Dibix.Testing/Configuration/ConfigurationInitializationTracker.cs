using System;

namespace Dibix.Testing
{
    public abstract class ConfigurationInitializationTracker : IConfigurationSectionHandler
    {
        public ConfigurationPropertyInitializationTracker PropertyInitializationTracker
        {
            protected internal get => field ?? throw new InvalidOperationException($"{nameof(PropertyInitializationTracker)} not initialized");
            set;
        }
        protected ConfigurationInitializationToken InitializationToken => PropertyInitializationTracker.InitializationToken;

        void IConfigurationSectionHandler.EnterSection(string path) => PropertyInitializationTracker.EnterSection(path);
    }
}