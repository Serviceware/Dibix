using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeArtifactsGenerationContext
    {
        public string ProjectDirectory { get; }
        public string Namespace { get; }
        public string DefaultOutputFilePath { get; }
        public string ClientOutputFilePath { get; }
        public ICollection<string> Sources { get; }
        public ICollection<string> Contracts { get; }
        public ICollection<string> Endpoints { get; }
        public ICollection<string> References { get; }
        public bool MultipleAreas { get; }
        public bool EmbedStatements { get; }
        public IErrorReporter ErrorReporter { get; }
        public ICollection<string> DetectedReferences { get; }

        public CodeArtifactsGenerationContext
        (
            string projectDirectory
          , string @namespace
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> sources
          , ICollection<string> contracts
          , ICollection<string> endpoints
          , ICollection<string> references
          , bool multipleAreas
          , bool embedStatements
          , IErrorReporter errorReporter
        )
        {
            this.ProjectDirectory = projectDirectory;
            this.Namespace = @namespace;
            this.DefaultOutputFilePath = defaultOutputFilePath;
            this.ClientOutputFilePath = clientOutputFilePath;
            this.Sources = sources;
            this.Contracts = contracts;
            this.Endpoints = endpoints;
            this.References = references;
            this.MultipleAreas = multipleAreas;
            this.EmbedStatements = embedStatements;
            this.ErrorReporter = errorReporter;
            this.DetectedReferences = new Collection<string>();
        }
    }
}