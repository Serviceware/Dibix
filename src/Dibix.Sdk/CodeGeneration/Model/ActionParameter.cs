namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public bool HasDefaultValue { get; }
        public object DefaultValue { get; }

        public ActionParameter(string name, TypeReference type, bool hasDefaultValue, object defaultValue)
        {
            this.Name = name;
            this.Type = type;
            this.HasDefaultValue = hasDefaultValue;
            this.DefaultValue = defaultValue;
        }
    }
}