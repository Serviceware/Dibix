using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ConfigurationAuthorizationTemplates
    {
        private readonly IDictionary<string, ConfigurationAuthorizationTemplate> _map = new Dictionary<string, ConfigurationAuthorizationTemplate>();

        public void Register(ConfigurationAuthorizationTemplate template) => _map.Add(template.Name, template);

        public bool TryGetTemplate(string name, out ConfigurationAuthorizationTemplate template) => _map.TryGetValue(name, out template);
    }
}