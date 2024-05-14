using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ParameterDescriptor : ExplicitParameter
    {
        public TypeReference Type { get; }
        public ActionParameterLocation? ParameterLocation { get; }
        public ValueReference DefaultValue { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }

        public ParameterDescriptor(JProperty property, TypeReference type, ActionParameterLocation? parameterLocation, ValueReference defaultValue, ActionParameterSourceBuilder sourceBuilder) : base(property)
        {
            Type = type;
            ParameterLocation = parameterLocation;
            DefaultValue = defaultValue;
            SourceBuilder = sourceBuilder;
        }
    }
}