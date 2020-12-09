namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation Location { get; }
        public bool HasDefaultValue { get; }
        public object DefaultValue { get; }
        public ActionParameterSource Source { get; }

        public ActionParameter(string name, TypeReference type, ActionParameterLocation location, bool hasDefaultValue, object defaultValue, ActionParameterSource source)
        {
            this.Name = name;
            this.Type = type;
            this.Location = location;
            this.HasDefaultValue = hasDefaultValue;
            this.DefaultValue = defaultValue;
            this.Source = source;
        }
    }
}