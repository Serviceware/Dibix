namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlElementValue
    {
        string Value { get; }
        int Line { get; }
        int Column { get; }
    }
}