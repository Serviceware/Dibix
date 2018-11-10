using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public class TestExecutionEnvironment : EmptyExecutionEnvironment
    {
        private readonly string _projectName;
        private readonly string _namespace;
        private readonly string _className;

        public TestExecutionEnvironment(string projectName, string @namespace, string className)
        {
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

        public override TypeInfo LoadType(IExecutionEnvironment environment, string typeName, string normalizedTypeName, Action<string> errorHandler)
        {
            //Type.GetType(typeName)
            TypeInfo info = new TypeInfo(typeName, false);
            return info;
        }
    }
}