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
        private readonly IControllerDefinitionProvider _controllerDefinitionProvider;
        private readonly IUserDefinedTypeProvider _userDefinedTypeProvider;

        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; } = "Dibix";
        public string ClassName => "SqlQueryAccessor";
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public StaticCodeGenerationContext(string projectDirectory, string @namespace, ICollection<string> artifacts, IEnumerable<string> contracts, IEnumerable<string> endpoints, bool multipleAreas, bool isDml, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            this._contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, errorReporter, contracts, multipleAreas);
            this._controllerDefinitionProvider = new ControllerDefinitionProvider(fileSystemProvider, errorReporter, endpoints);
            this._userDefinedTypeProvider = new UserDefinedTypeProvider(artifacts, errorReporter, multipleAreas);

            this.Configuration = new GeneratorConfiguration();
            this.Configuration.Output.GeneratePublicArtifacts = true;
            if (this._contractDefinitionProvider.HasSchemaErrors || this._controllerDefinitionProvider.HasSchemaErrors)
            {
                errorReporter.ReportErrors();
                this.Configuration.IsInvalid = true;
            }

            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, null, multipleAreas, this.Configuration.Output.GeneratePublicArtifacts);
            if (!isDml)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            artifacts.Where(x => MatchFile(projectDirectory, x, isDml, errorReporter)).Each(source.Include);
            this.Configuration.Input.Sources.Add(source);

            this.ContractResolverFacade = new ContractResolverFacade(new UnsupportedAssemblyLocator());
            this.ContractResolverFacade.RegisterContractResolver(new ContractDefinitionResolver(this._contractDefinitionProvider), 0);
            this.ErrorReporter = errorReporter;
        }

        public void CollectAdditionalArtifacts(SourceArtifacts artifacts)
        {
            artifacts.Contracts.AddRange(this._contractDefinitionProvider.Contracts);
            artifacts.UserDefinedTypes.AddRange(this._userDefinedTypeProvider.Types);
            artifacts.Controllers.AddRange(this._controllerDefinitionProvider.Controllers);
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath, bool isDML, IErrorReporter errorReporter)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            ICollection<SqlHint> hints = SqlHintParser.FromFile(inputFilePath, errorReporter).ToArray();
            bool hasHints = hints.Any();
            bool hasNoCompileHint = hints.Any(x => x.Kind == SqlHint.NoCompile);
            return (isDML || hasHints) && !hasNoCompileHint;
        }
    }
}