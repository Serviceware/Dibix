using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IControllerActionTargetSelector
    {
        ActionDefinitionTarget Select(string target, string filePath, IJsonLineInfo lineInfo);
    }
}