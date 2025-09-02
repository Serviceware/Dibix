namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string ApiParameterName { get; }
        public string InternalParameterName { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation ParameterLocation { get; }
        public ValueReference DefaultValue { get; }
        public string Description { get; }
        public ActionParameterSource ParameterSource { get; }
        public bool IsRequired { get; }
        public bool IsOutput { get; }
        public SourceLocation SourceLocation { get; }

        public ActionParameter(string apiParameterName, string internalParameterName, TypeReference type, ActionParameterLocation location, bool isRequired, bool isOutput, ValueReference defaultValue, string description, ActionParameterSource source, SourceLocation sourceLocation)
        {
            ApiParameterName = apiParameterName;
            InternalParameterName = internalParameterName;
            Type = type;
            ParameterLocation = location;
            IsRequired = isRequired;
            IsOutput = isOutput;
            DefaultValue = defaultValue;
            Description = description;
            ParameterSource = source;
            SourceLocation = sourceLocation;
        }
    }
}