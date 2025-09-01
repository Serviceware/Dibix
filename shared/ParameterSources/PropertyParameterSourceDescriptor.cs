using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class PropertyParameterSourceDescriptor : IPropertyDescriptor
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public IReadOnlyCollection<string> RequiredClaims { get; }

        public PropertyParameterSourceDescriptor(string name, TypeReference type) : this(name, type, []) { }
        public PropertyParameterSourceDescriptor(string name, TypeReference type, IEnumerable<string> requiredClaims)
        {
            Name = name;
            Type = type;
            RequiredClaims = requiredClaims.ToArray();
        }
    }
}