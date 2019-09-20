using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class StaticCodeGenerationContext : ICodeGenerationContext
    {
        private readonly IEnumerable<ContractDefinition> _contracts;
        private readonly IEnumerable<ControllerDefinition> _controllers;
        private readonly CodeArtifactKind _codeArtifactKind;
        private readonly IUserDefinedTypeProvider _userDefinedTypeProvider;

        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; } = "Dibix";
        public string ClassName => "SqlQueryAccessor";
        public IContractResolverFacade ContractResolverFacade { get; }
        public IErrorReporter ErrorReporter { get; }

        public StaticCodeGenerationContext
        (
            string projectDirectory
          , string @namespace
          , ICollection<string> sources
          , IEnumerable<ContractDefinition> contracts
          , IEnumerable<ControllerDefinition> endpoints
          , IEnumerable<string> references
          , CodeArtifactKind codeArtifactKind
          , bool multipleAreas
          , bool embedStatements
          , IFileSystemProvider fileSystemProvider
          , IContractDefinitionProvider contractDefinitionProvider
          , IErrorReporter errorReporter)
        {
            this._contracts = contracts;
            this._controllers = endpoints;
            this._codeArtifactKind = codeArtifactKind;
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            this._userDefinedTypeProvider = new UserDefinedTypeProvider(sources, errorReporter, multipleAreas);

            this.Configuration = new GeneratorConfiguration();
            this.Configuration.Output.GeneratePublicArtifacts = true;
            if (codeArtifactKind == CodeArtifactKind.Client)
                this.Configuration.Output.Writer = typeof(ClientContractCSWriter);

            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, null, multipleAreas, this.Configuration.Output.GeneratePublicArtifacts);
            if (!embedStatements)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            if (codeArtifactKind == CodeArtifactKind.Server)
            {
                sources.Where(x => MatchFile(projectDirectory, x, embedStatements, errorReporter)).Each(source.Include);
                this.Configuration.Input.Sources.Add(source);
            }

            this.ContractResolverFacade = new ContractResolverFacade(new DefaultAssemblyLocator(projectDirectory, references));
            this.ContractResolverFacade.RegisterContractResolver(new ContractDefinitionResolver(contractDefinitionProvider), 0);
            this.ErrorReporter = errorReporter;
        }

        public void CollectAdditionalArtifacts(SourceArtifacts artifacts)
        {
            artifacts.Contracts.AddRange(this._contracts);

            if (this._codeArtifactKind == CodeArtifactKind.Server)
            {
                artifacts.UserDefinedTypes.AddRange(this._userDefinedTypeProvider.Types);
                artifacts.Controllers.AddRange(this._controllers);
            }
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath, bool embedStatements, IErrorReporter errorReporter)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            ICollection<SqlHint> hints = SqlHintParser.FromFile(inputFilePath, errorReporter).ToArray();
            bool hasHints = hints.Any();
            bool hasNoCompileHint = hints.Any(x => x.Kind == SqlHint.NoCompile);
            return (embedStatements || hasHints) && !hasNoCompileHint;
        }
    }
}