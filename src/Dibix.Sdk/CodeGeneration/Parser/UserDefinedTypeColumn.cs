namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeColumn
    {
        public string Name { get; }
        public string Type { get; }

        public UserDefinedTypeColumn(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}