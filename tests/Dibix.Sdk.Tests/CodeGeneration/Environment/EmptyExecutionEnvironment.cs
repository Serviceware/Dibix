using System;
using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.CodeGeneration;
using TypeInfo = Dibix.Sdk.CodeGeneration.TypeInfo;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public abstract class EmptyExecutionEnvironment
    {
        public virtual string GetCurrentDirectory() => Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "CodeGeneration"));

        public virtual string GetProjectName() => null;

        public virtual string GetProjectDefaultNamespace() => null;

        public virtual string GetClassName() => null;

        public virtual void VerifyProject(string projectName) { }

        public virtual bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath)
        {
            assemblyPath = null;
            return false;
        }

        public virtual void RegisterError(string fileName, int line, int column, string errorNumber, string errorText) { }

        public virtual string GetPhysicalFilePath(string projectName, VirtualPath virtualPath) => null;

        public virtual IEnumerable<string> GetFiles(string projectName, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude) { yield break; }

        public virtual bool ReportErrors() => false;

        public virtual TypeInfo LoadType(TypeName typeName, Action<string> errorHandler) => throw new NotImplementedException();
    }
}