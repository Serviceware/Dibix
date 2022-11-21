using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ConfigurationAuthorizationTemplate
    {
        public string Name { get; }
        public JObject Content { get; }

        public ConfigurationAuthorizationTemplate(string name, JObject content)
        {
            Name = name;
            Content = content;
        }
    }
}