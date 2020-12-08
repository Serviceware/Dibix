using System;

namespace Dibix.Http
{
    public sealed class HttpActionParameter
    {
        public string Name { get; }
        public Type Type { get; }
        public HttpParameterLocation Location { get; set; }
        public bool IsOptional { get; }

        public HttpActionParameter(string name, Type type, HttpParameterLocation location, bool isOptional)
        {
            this.Name = name;
            this.Type = type;
            this.Location = location;
            this.IsOptional = isOptional;
        }
    }
}