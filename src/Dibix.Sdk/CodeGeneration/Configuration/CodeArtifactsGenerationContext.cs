using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerationContext
    {
        public string ProjectDirectory { get; }
        public string ProductName { get; }
        public string AreaName { get; }
        public string RootNamespace { get; }
        public string DefaultOutputFilePath { get; }
        public string DefaultOutputName { get; }
        public string ClientOutputFilePath { get; }
        public GeneratorConfiguration Configuration { get; }
        public ICollection<string> Sources { get; }
        public ICollection<SqlStatementInfo> Statements { get; }
        public ICollection<ContractDefinition> Contracts => this.ContractDefinitionProvider.Contracts;
        public ICollection<ControllerDefinition> Controllers { get; }
        public ICollection<UserDefinedTypeDefinition> UserDefinedTypes { get; }
        public ICollection<string> References { get; }
        public bool EmbedStatements { get; }
        public ICollection<string> DetectedReferences { get; }
        public IFileSystemProvider FileSystemProvider { get; }
        public IContractDefinitionProvider ContractDefinitionProvider { get; }
        public IContractResolverFacade ContractResolver { get; }
        public IErrorReporter ErrorReporter { get; }

        public CodeArtifactsGenerationContext
        (
            string projectDirectory
          , string productName
          , string areaName
          , string rootNamespace
          , string defaultOutputFilePath
          , string defaultOutputName
          , string clientOutputFilePath
          , GeneratorConfiguration configuration
          , ICollection<SqlStatementInfo> statements
          , ICollection<string> sources
          , ICollection<ControllerDefinition> controllers
          , ICollection<UserDefinedTypeDefinition> userDefinedTypes
          , ICollection<string> references
          , bool embedStatements
          , IFileSystemProvider fileSystemProvider
          , IContractDefinitionProvider contractDefinitionProvider
          , IContractResolverFacade contractResolver
          , IErrorReporter errorReporter
        )
        {
            this.ProjectDirectory = projectDirectory;
            this.ProductName = productName;
            this.AreaName = areaName;
            this.RootNamespace = rootNamespace;
            this.DefaultOutputFilePath = defaultOutputFilePath;
            this.DefaultOutputName = defaultOutputName;
            this.ClientOutputFilePath = clientOutputFilePath;
            this.Configuration = configuration;
            this.Statements = statements;
            this.Sources = sources;
            this.References = references;
            this.EmbedStatements = embedStatements;
            this.FileSystemProvider = fileSystemProvider;
            this.ContractDefinitionProvider = contractDefinitionProvider;
            this.ContractResolver = contractResolver;
            this.ErrorReporter = errorReporter;
            this.DetectedReferences = new Collection<string>();
            this.Controllers = controllers;
            this.UserDefinedTypes = userDefinedTypes;
        }
    }
}