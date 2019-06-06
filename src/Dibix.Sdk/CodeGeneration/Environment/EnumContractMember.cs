namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumContractMember
    {
        public string Name { get; }
        public string Value { get; }

        public EnumContractMember(string name) : this(name, null) { }
        public EnumContractMember(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}