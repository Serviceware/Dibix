using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CompileDataAccessArtifactsTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string @namespace
          , string outputFilePath
          , ICollection<string> sources
          , ICollection<string> contracts
          , ICollection<string> endpoints
          , ICollection<string> references
          , bool multipleAreas
          , bool embedStatements
          , string clientOutputFilePath
          , TaskLoggingHelper logger
          , out string[] detectedReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            bool failed = false;
            ICollection<string> detectedOutputReferences = new Collection<string>();

            if (!Execute(projectDirectory, @namespace, outputFilePath, sources, contracts, endpoints, references, CompilationMode.Server, multipleAreas, embedStatements, logger, detectedOutputReferences, errorReporter))
                failed = true;

            if (!String.IsNullOrEmpty(clientOutputFilePath))
            {
                if (!Execute(projectDirectory, @namespace, clientOutputFilePath, sources, contracts, endpoints, references, CompilationMode.Client, multipleAreas, embedStatements, logger, detectedOutputReferences, errorReporter))
                    failed = true;
            }

            detectedReferences = detectedOutputReferences.ToArray();
            return !failed;
        }

        private static bool Execute(string projectDirectory, string @namespace, string outputFilePath, ICollection<string> sources, ICollection<string> contracts, ICollection<string> endpoints, ICollection<string> references, CompilationMode compilationMode, bool multipleAreas, bool embedStatements, TaskLoggingHelper logger, ICollection<string> detectedReferences, IErrorReporter errorReporter)
        {
            StaticCodeGenerationContext context = new StaticCodeGenerationContext(projectDirectory, @namespace, sources ?? new string[0], contracts ?? new string[0], endpoints ?? new string[0], references ?? new string[0], compilationMode, multipleAreas, embedStatements, errorReporter);
            ICodeGenerator generator = new DaoCodeGenerator(context);

            string generated = generator.Generate();
            detectedReferences.AddRange(context.Configuration.Output.DetectedReferences);
            if (!logger.HasLoggedErrors)
            {
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }
    }
}