namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ApiParameter
    {
        public string ParameterName { get; }
        public string TargetParameterName { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation ParameterLocation { get; }
        public ValueReference DefaultValue { get; }
        public string Description { get; }
        public ActionParameterSource ParameterSource { get; }
        public bool IsRequired { get; }
        public bool IsOutput { get; }
        public SourceLocation SourceLocation { get; }

        public ApiParameter(string parameterName, string targetParameterName, TypeReference type, ActionParameterLocation parameterLocation, ValueReference defaultValue, string description, ActionParameterSource parameterSource, bool isRequired, bool isOutput, SourceLocation sourceLocation)
        {
            ParameterName = parameterName;
            TargetParameterName = targetParameterName;
            Type = type;
            ParameterLocation = parameterLocation;
            DefaultValue = defaultValue;
            Description = description;
            ParameterSource = parameterSource;
            IsRequired = isRequired;
            IsOutput = isOutput;
            SourceLocation = sourceLocation;
        }
    }
}