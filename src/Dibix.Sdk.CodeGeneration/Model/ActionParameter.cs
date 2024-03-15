using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string ApiParameterName { get; }
        public string InternalParameterName { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation ParameterLocation { get; }
        public ValueReference DefaultValue { get; }
        public ActionParameterSource ParameterSource { get; }
        public bool IsRequired { get; }
        public bool IsOutput { get; }
        public SourceLocation SourceLocation { get; }

        public ActionParameter(string apiParameterName, string internalParameterName, TypeReference type, ActionParameterLocation location, bool isRequired, bool isOutput, ValueReference defaultValue, ActionParameterSource source, SourceLocation sourceLocation)
        {
            ApiParameterName = apiParameterName;
            InternalParameterName = internalParameterName;
            Type = type;
            ParameterLocation = location;
            IsRequired = isRequired;
            IsOutput = isOutput;
            DefaultValue = defaultValue;
            ParameterSource = source;
            SourceLocation = sourceLocation;
        }
    }
}