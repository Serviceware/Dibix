using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dibix.Sdk.Tests
{
    public abstract class EmptyExecutionEnvironment : IExecutionEnvironment
    {
        protected static readonly string TestsProjectDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));
        protected static readonly string TestsRootDirectory = Path.GetFullPath(Path.Combine(TestsProjectDirectory, ".."));

        public virtual string GetCurrentDirectory() => TestsProjectDirectory;

        public virtual string GetProjectName() => null;

        public virtual string GetProjectDefaultNamespace() => null;

        public virtual string GetClassName() => null;

        public virtual void VerifyProject(string projectName) { }

        public virtual Assembly LoadAssembly(string assemblyName) => null;

        public virtual void RegisterError(string fileName, int line, int column, string errorNumber, string errorText) { }

        public virtual IEnumerable<string> GetFilesInProject(string projectName, string virtualFolderPath, bool recursive, IEnumerable<string> excludedFolders) { yield break; }

        public virtual string GetPhysicalFilePath(string projectName, string virtualFilePath) => Path.Combine(TestsRootDirectory, projectName, virtualFilePath.Replace('/', '\\'));

        public virtual bool ReportErrors() => false;

        public virtual TypeInfo LoadType(IExecutionEnvironment environment, string typeName, string normalizedTypeName, Action<string> errorHandler) => throw new NotImplementedException();
    }
}