namespace Dibix
{
    public interface IPropertyDescriptor
    {
        string Name { get; }
        TypeReference Type { get; }
    }
}