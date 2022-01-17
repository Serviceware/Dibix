namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlElement
    {
        string Source { get; }
        int Line { get; }
        int Column { get; }
        
        bool TryGetPropertyValue(string propertyName, bool isDefault, out ISqlElementValue value);
        ISqlElementValue GetPropertyValue(string name);
    }
}