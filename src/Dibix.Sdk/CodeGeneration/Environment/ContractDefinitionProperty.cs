namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractDefinitionProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public ContractDefinitionProperty(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}