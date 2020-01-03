using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class StaticCodeGenerationContext : ICodeGenerationContext
    {
        private readonly IEnumerable<SqlStatementInfo> _statements;
        private readonly IEnumerable<ContractDefinition> _contracts;
        private readonly IEnumerable<ControllerDefinition> _controllers;
        private readonly IEnumerable<UserDefinedTypeDefinition> _userDefinedTypes;
        private readonly CodeArtifactKind _codeArtifactKind;

        public GeneratorConfiguration Configuration { get; }
        public string RootNamespace { get; }
        public string DefaultClassName { get; } = "SqlQueryAccessor";
        public IContractResolverFacade ContractResolver { get; }
        public IErrorReporter ErrorReporter { get; }

        public StaticCodeGenerationContext
        (
            string rootNamespace
          , string defaultOutputName
          , GeneratorConfiguration generatorConfiguration
          , IEnumerable<SqlStatementInfo> statements
          , IEnumerable<ContractDefinition> contracts
          , IEnumerable<ControllerDefinition> endpoints
          , IEnumerable<UserDefinedTypeDefinition> userDefinedTypes
          , CodeArtifactKind codeArtifactKind
          , IContractResolverFacade contractResolver
          , IErrorReporter errorReporter)
        {
            this._statements = statements;
            this._contracts = contracts;
            this._controllers = endpoints;
            this._userDefinedTypes = userDefinedTypes;
            this._codeArtifactKind = codeArtifactKind;
            this.Configuration = generatorConfiguration;
            this.RootNamespace = rootNamespace;
            this.DefaultClassName = defaultOutputName;
            this.ContractResolver = contractResolver;
            this.ErrorReporter = errorReporter;

            if (codeArtifactKind == CodeArtifactKind.Client)
                this.Configuration.Output.Writer = typeof(ClientContractCSWriter);
        }

        public void CollectAdditionalArtifacts(SourceArtifacts artifacts)
        {
            artifacts.Contracts.AddRange(this._contracts);

            if (this._codeArtifactKind == CodeArtifactKind.Server)
            {
                artifacts.Statements.AddRange(this._statements);
                artifacts.UserDefinedTypes.AddRange(this._userDefinedTypes);
                artifacts.Controllers.AddRange(this._controllers);
            }
        }
    }
}