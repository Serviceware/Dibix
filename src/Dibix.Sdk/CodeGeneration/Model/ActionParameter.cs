namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string ApiParameterName { get; }
        public string InternalParameterName { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation Location { get; }
        public DefaultValue DefaultValue { get; }
        public ActionParameterSource Source { get; }
        public bool IsRequired { get; }

        public ActionParameter(string apiParameterName, string internalParameterName, TypeReference type, ActionParameterLocation location, bool isRequired, DefaultValue defaultValue, ActionParameterSource source)
        {
            this.ApiParameterName = apiParameterName;
            this.InternalParameterName = internalParameterName;
            this.Type = type;
            this.Location = location;
            this.IsRequired = isRequired;
            this.DefaultValue = defaultValue;
            this.Source = source;
        }
    }
}