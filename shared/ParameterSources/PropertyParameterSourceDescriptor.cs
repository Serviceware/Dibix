namespace Dibix
{
    internal sealed class PropertyParameterSourceDescriptor : IPropertyDescriptor
    {
        public string Name { get; }
        public TypeReference Type { get; }

        public PropertyParameterSourceDescriptor(string name, TypeReference type)
        {
            Name = name;
            Type = type;
        }
    }
}