using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class GlobalCodeGenerationContext : ICodeGenerationContext
    {
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; } = "Dibix";
        public string ClassName => "SqlQueryAccessor";
        public ITypeLoaderFacade TypeLoaderFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public GlobalCodeGenerationContext(string projectDirectory, string @namespace, IEnumerable<string> inputs, IAssemblyLocator assemblyLocator, bool isDml, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);

            this.Configuration = new GeneratorConfiguration();
            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, null);
            if (!isDml)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            inputs.Where(x => MatchFile(projectDirectory, x)).Each(source.Include);
            this.Configuration.Input.Sources.Add(source);

            this.TypeLoaderFacade = new TypeLoaderFacade(new TypeLoader(), assemblyLocator);
            this.ErrorReporter = errorReporter;
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);

            using (Stream stream = File.OpenRead(inputFilePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    if (textReader.ReadLine().StartsWith("-- @"))
                        return true;
                }
            }

            return false;
        }

        private class TypeLoader : ITypeLoader
        {
            public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
            {
                errorHandler($"The type must be qualified with an assembly name: {typeName}");
                return null;
            }
        }
    }
}