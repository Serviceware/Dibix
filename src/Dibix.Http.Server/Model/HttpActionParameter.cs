using System;

namespace Dibix.Http.Server
{
    public sealed class HttpActionParameter
    {
        public string Name { get; }
        public Type Type { get; }
        public HttpParameterLocation Location { get; set; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        public HttpActionParameter(string name, Type type, HttpParameterLocation location, bool isOptional, object defaultValue)
        {
            Name = name;
            Type = type;
            Location = location;
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
    }
}