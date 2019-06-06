using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class StaticCodeGenerationContext : ICodeGenerationContext
    {
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        private readonly IUserDefinedTypeProvider _userDefinedTypeProvider;

        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; } = "Dibix";
        public string ClassName => "SqlQueryAccessor";
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public StaticCodeGenerationContext(string projectDirectory, string @namespace, ICollection<string> artifacts, IEnumerable<string> contracts, bool multipleAreas, string contractsLayerName, bool isDml, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            this._contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, errorReporter, contracts, multipleAreas, contractsLayerName);
            this._userDefinedTypeProvider = new UserDefinedTypeProvider(artifacts);

            this.Configuration = new GeneratorConfiguration();
            this.Configuration.Output.GeneratePublicArtifacts = true;
            if (this._contractDefinitionProvider.HasSchemaErrors)
            {
                errorReporter.ReportErrors();
                this.Configuration.IsInvalid = true;
            }

            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, null, multipleAreas, contractsLayerName);
            if (!isDml)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            artifacts.Where(x => MatchFile(projectDirectory, x)).Each(source.Include);
            this.Configuration.Input.Sources.Add(source);

            this.ContractResolverFacade = new ContractResolverFacade(new UnsupportedAssemblyLocator());
            this.ContractResolverFacade.RegisterContractResolver(new ContractDefinitionResolver(this._contractDefinitionProvider), 0);
            this.ErrorReporter = errorReporter;
        }

        public void CollectAdditionalArtifacts(SourceArtifacts artifacts)
        {
            artifacts.Contracts.AddRange(this._contractDefinitionProvider.Contracts);
            artifacts.UserDefinedTypes.AddRange(this._userDefinedTypeProvider.Types);
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            using (Stream stream = File.OpenRead(inputFilePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    while (true)
                    {
                        string line = textReader.ReadLine();
                        if (line == null)
                            return false;

                        if (line.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
                            return false;

                        if (line.StartsWith("-- @", StringComparison.Ordinal))
                            return true;
                    }
                }
            }
        }
    }
}