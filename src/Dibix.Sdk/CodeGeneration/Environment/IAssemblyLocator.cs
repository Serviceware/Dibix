namespace Dibix.Sdk.CodeGeneration
{
    public interface IAssemblyLocator
    {
        bool TryGetAssemblyLocation(string assemblyName, out string path);
    }
}