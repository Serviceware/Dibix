using System.Reflection;

namespace Dibix.Sdk
{
    public interface IExecutionEnvironment : IFileSystemProvider, ITypeLoader
    {
        string GetCurrentDirectory();
        string GetProjectName();
        string GetProjectDefaultNamespace();
        string GetClassName();
        void VerifyProject(string projectName);
        Assembly LoadAssembly(string assemblyName);
        void RegisterError(string fileName, int line, int column, string errorNumber, string errorText);
        bool ReportErrors();
    }
}