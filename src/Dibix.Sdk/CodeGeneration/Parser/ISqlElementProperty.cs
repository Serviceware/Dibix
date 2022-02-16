namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlElementProperty
    {
        string Name { get; }
        ISqlElementValue Value { get; }
        int Line { get; }
        int Column { get; }
    }
}