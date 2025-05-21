namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterPropertySource
    {
        ActionParameterSourceDefinition Definition { get; }
        string PropertyPath { get; }
        SourceLocation Location { get; }
    }
}