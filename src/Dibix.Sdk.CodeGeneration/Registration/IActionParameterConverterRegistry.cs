namespace Dibix.Sdk.CodeGeneration
{
    public interface IActionParameterConverterRegistry
    {
        bool IsRegistered(string name);
        void Register(string name);
    }
}