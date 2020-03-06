using System;

namespace Dibix.Http
{
    public sealed class HttpActionParameter
    {
        public string Name { get; }
        public Type Type { get; }
        public bool IsOptional { get; }

        public HttpActionParameter(string name, Type type, bool isOptional)
        {
            this.Name = name;
            this.Type = type;
            this.IsOptional = isOptional;
        }
    }
}