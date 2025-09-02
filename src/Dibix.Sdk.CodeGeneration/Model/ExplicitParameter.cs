using System;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExplicitParameter
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation? ParameterLocation { get; }
        public Func<TypeReference, ValueReference> DefaultValueBuilder { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public string Description { get; }
        public SourceLocation SourceLocation { get; }
        public bool Visited { get; set; }

        public ExplicitParameter(JProperty property, TypeReference type, ActionParameterLocation? parameterLocation, Func<TypeReference, ValueReference> defaultValueBuilder, ActionParameterSourceBuilder sourceBuilder, string description)
        {
            Type = type;
            ParameterLocation = parameterLocation;
            DefaultValueBuilder = defaultValueBuilder;
            SourceBuilder = sourceBuilder;
            Description = description;
            Name = property.Name;
            SourceLocation = property.GetSourceInfo();
        }
    }
}