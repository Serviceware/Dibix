namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchemaMember
    {
        public string Name { get; }
        public int ActualValue { get; }
        public string StringValue { get; }

        public EnumSchemaMember(string name, int actualValue, string stringValue)
        {
            this.Name = name;
            this.ActualValue = actualValue;
            this.StringValue = stringValue;
        }
    }
}