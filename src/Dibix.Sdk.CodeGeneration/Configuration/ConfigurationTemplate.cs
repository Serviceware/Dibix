using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ConfigurationTemplate
    {
        public string Name { get; }
        public JObject Action { get; set; }

        public ConfigurationTemplate(string name)
        {
            Name = name;
        }
    }
}