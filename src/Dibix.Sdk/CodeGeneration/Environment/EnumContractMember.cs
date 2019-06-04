namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumContractMember
    {
        public string Name { get; }
        public int? Value { get; }

        public EnumContractMember(string name) : this(name, null) { }
        public EnumContractMember(string name, int value) : this(name, (int?)value) { }
        private EnumContractMember(string name, int? value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}