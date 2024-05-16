namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterPropertySource
    {
        ActionParameterSourceDefinition Definition { get; }
        string PropertyName { get; }
        SourceLocation Location { get; }
    }
}