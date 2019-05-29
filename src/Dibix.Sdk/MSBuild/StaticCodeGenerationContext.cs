﻿using System;
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

        public StaticCodeGenerationContext(string projectDirectory, string @namespace, ICollection<string> artifacts, IEnumerable<string> contracts, IAssemblyLocator assemblyLocator, bool isDml, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(@namespace))
                this.Namespace = @namespace;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            this._contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, contracts);
            this._userDefinedTypeProvider = new UserDefinedTypeProvider(artifacts);

            this.Configuration = new GeneratorConfiguration();
            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(fileSystemProvider, null);
            if (!isDml)
                source.Formatter = typeof(ExecStoredProcedureSqlStatementFormatter);

            artifacts.Where(x => MatchFile(projectDirectory, x)).Each(source.Include);
            this.Configuration.Input.Sources.Add(source);

            this.ContractResolverFacade = new ContractResolverFacade(assemblyLocator);
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
                    if (textReader.ReadLine().StartsWith("-- @"))
                        return true;
                }
            }

            return false;
        }
    }
}