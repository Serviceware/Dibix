namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlElementProperty
    {
        Token<string> Name { get; }
        Token<string> Value { get; }
    }
}