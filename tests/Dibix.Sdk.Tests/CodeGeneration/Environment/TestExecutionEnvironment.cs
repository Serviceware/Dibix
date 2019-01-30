using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.CodeGeneration;
using TypeInfo = Dibix.Sdk.CodeGeneration.TypeInfo;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public class TestExecutionEnvironment : EmptyExecutionEnvironment
    {
        private readonly string _projectName;
        private readonly string _namespace;
        private readonly string _className;
        private readonly ICollection<CompilerError> _errors;

        public TestExecutionEnvironment(string projectName, string @namespace, string className)
        {
            this._errors = new Collection<CompilerError>();
            this._projectName = projectName;
            this._namespace = @namespace;
            this._className = className;
        }

        public override IEnumerable<string> GetFilesInProject(string projectName, string virtualFolderPath, bool recursive, IEnumerable<string> excludedFolders)
        {
            string directory = Path.GetFullPath(Path.Combine(TestsRootDirectory, projectName, virtualFolderPath ?? String.Empty));
            string[] paths = Directory.EnumerateFiles(directory, "*.sql", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                      .Where(x => !excludedFolders.Any(y => x.Substring(directory.Length + 1).StartsWith(y.Replace("/", "\\").TrimStart('\\'), StringComparison.OrdinalIgnoreCase)))
                                      .ToArray();

            return paths;
        }

        public override string GetPhysicalFilePath(string projectName, string virtualFilePath)
        {
            string path = Path.GetFullPath(Path.Combine(TestsRootDirectory, projectName, virtualFilePath));
            return path;
        }

        public override string GetProjectName()
        {
            return this._projectName;
        }

        public override string GetProjectDefaultNamespace()
        {
            return this._namespace;
        }

        public override string GetClassName()
        {
            return this._className;
        }

        public override bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath)
        {
            Assembly assembly = this.GetType().Assembly;
            if (assemblyName == assembly.GetName().Name)
            {
                assemblyPath = assembly.Location;
                return true;
            }
            assemblyPath = null;
            return false;
        }

        public override void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            this._errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
        }

        public override bool ReportErrors()
        {
            if (this._errors.Any())
                throw new CodeGenerationException(this._errors);

            return false;
        }

        public override TypeInfo LoadType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler)
        {
            Type type = Type.GetType($"{typeName.NormalizedTypeName},{this.GetType().Assembly}", true);
            return  TypeInfo.FromClrType(type, typeName);
        }
    }
}