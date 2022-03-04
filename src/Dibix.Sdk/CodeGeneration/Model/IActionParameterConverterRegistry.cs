namespace Dibix.Sdk.CodeGeneration
{
    internal interface IActionParameterConverterRegistry
    {
        bool IsRegistered(string name);
        void Register(string name);
    }
}