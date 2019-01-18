namespace Dibix.Sdk.CodeGeneration
{
    public interface IExecutionEnvironment : IFileSystemProvider, ITypeLoader
    {
        string GetCurrentDirectory();
        string GetProjectName();
        string GetProjectDefaultNamespace();
        string GetClassName();
        void VerifyProject(string projectName);
        bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath);
        void RegisterError(string fileName, int line, int column, string errorNumber, string errorText);
        bool ReportErrors();
    }
}