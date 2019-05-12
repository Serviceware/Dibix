using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class JsonContract
    {
        public string Namespace { get; set; }
        public string DefinitionName { get; }
        public JSchema Schema { get; }

        public JsonContract(string @namespace, string definitionName, JSchema schema)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
            this.Schema = schema;
        }
    }
}