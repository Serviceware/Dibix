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
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public GlobalCodeGenerationContext(string projectDirectory, string @namespace, IEnumerable<string> inputs, IAssemblyLocator assemblyLocator, bool isDml, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider);

            this.Configuration = new GeneratorConfiguration();
            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, contractDefinitionProvider, null);
            if (!isDml)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            inputs.Where(x => MatchFile(projectDirectory, x)).Each(source.Include);
            this.Configuration.Input.Sources.Add(source);

            this.ContractResolverFacade = new ContractResolverFacade();
            this.ContractResolverFacade.RegisterContractResolver(new ContractDefinitionResolver(contractDefinitionProvider));
            this.ContractResolverFacade.RegisterContractResolver(new ClrTypeContractResolver());
            this.ContractResolverFacade.RegisterContractResolver(new ForeignAssemblyTypeContractResolver(assemblyLocator));
            this.ErrorReporter = errorReporter;
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            if (!inputFilePath.EndsWith("hlsysapprovalfulfillment_getpending.sql"))
                return false;

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
    }
}