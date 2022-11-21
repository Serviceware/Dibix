using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ConfigurationTemplates
    {
        private const string DefaultTemplateKey = "Default";
        private readonly IDictionary<string, ConfigurationTemplate> _map = new Dictionary<string, ConfigurationTemplate>();

        public ConfigurationTemplate Default => _map.TryGetValue(DefaultTemplateKey, out ConfigurationTemplate template) ? template : new ConfigurationTemplate(DefaultTemplateKey);
        public ConfigurationAuthorizationTemplates Authorization { get; } = new ConfigurationAuthorizationTemplates();

        public void Register(ConfigurationTemplate template) => _map.Add(template.Name, template);
    }
}