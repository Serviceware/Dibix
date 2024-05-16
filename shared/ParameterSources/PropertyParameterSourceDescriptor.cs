namespace Dibix
{
    internal readonly struct PropertyParameterSourceDescriptor
    {
        public string Name { get; }
        public PrimitiveTypeReference Type { get; }

        public PropertyParameterSourceDescriptor(string name, PrimitiveTypeReference type)
        {
            Name = name;
            Type = type;
        }
    }
}