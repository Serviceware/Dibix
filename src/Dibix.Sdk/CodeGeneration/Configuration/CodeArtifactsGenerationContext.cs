﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerationContext
    {
        public string ProjectDirectory { get; }
        public string ProductName { get; }
        public string AreaName { get; }
        public string DefaultOutputFilePath { get; }
        public string ClientOutputFilePath { get; }
        public ICollection<string> Sources { get; }
        public ICollection<ContractDefinition> Contracts => this.ContractDefinitionProvider.Contracts;
        public ICollection<ControllerDefinition> Controllers { get; }
        public ICollection<string> References { get; }
        public bool EmbedStatements { get; }
        public ICollection<string> DetectedReferences { get; }
        public IFileSystemProvider FileSystemProvider { get; }
        public IContractDefinitionProvider ContractDefinitionProvider { get; }
        public IErrorReporter ErrorReporter { get; }

        public CodeArtifactsGenerationContext
        (
            string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> sources
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , ICollection<string> references
          , bool embedStatements
          , IErrorReporter errorReporter
        )
        {
            this.ProjectDirectory = projectDirectory;
            this.ProductName = productName;
            this.AreaName = areaName;
            this.DefaultOutputFilePath = defaultOutputFilePath;
            this.ClientOutputFilePath = clientOutputFilePath;
            this.Sources = sources;
            this.References = references;
            this.EmbedStatements = embedStatements;
            this.FileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            this.ErrorReporter = errorReporter;
            this.DetectedReferences = new Collection<string>();

            ContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(this.FileSystemProvider, errorReporter, contracts, productName, areaName);
            ControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(this.FileSystemProvider, errorReporter, endpoints);

            if (contractDefinitionProvider.HasSchemaErrors || controllerDefinitionProvider.HasSchemaErrors)
            {
                errorReporter.ReportErrors();
            }

            this.ContractDefinitionProvider = contractDefinitionProvider;
            this.Controllers = controllerDefinitionProvider.Controllers;
        }
    }
}