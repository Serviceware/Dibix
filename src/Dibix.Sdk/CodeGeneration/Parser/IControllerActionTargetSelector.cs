namespace Dibix.Sdk.CodeGeneration
{
    public interface IControllerActionTargetSelector
    {
        ActionDefinitionTarget Select(string target, string filePath, int line, int column);
    }
}