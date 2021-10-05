using System;

namespace Dibix.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ConfigurationProfileAttribute : Attribute
    {
        public string ProfileName { get; }

        public ConfigurationProfileAttribute(string profileName) => this.ProfileName = profileName;
    }
}