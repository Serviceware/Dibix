using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EndpointParameterSource
    {
        public string Name { get; }
        public bool IsDynamic { get; set; }
        public ICollection<string> Properties { get; }

        public EndpointParameterSource(string name)
        {
            this.Name = name;
            this.Properties = new HashSet<string>();
        }
        public EndpointParameterSource(string name, bool isDynamic) : this(name)
        {
            this.IsDynamic = isDynamic;
        }
    }
}