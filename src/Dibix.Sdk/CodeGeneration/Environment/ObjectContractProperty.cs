namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContractProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public ObjectContractProperty(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}