using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CodeGenerationTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> sources
          , ICollection<string> contracts
          , ICollection<string> endpoints
          , ICollection<string> references
          , bool multipleAreas
          , bool embedStatements
          , TaskLoggingHelper logger
          , out string[] detectedReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            CodeArtifactsGenerationContext context = new CodeArtifactsGenerationContext
            (
                projectDirectory
              , productName
              , areaName
              , defaultOutputFilePath
              , clientOutputFilePath
              , sources ?? new string[0]
              , contracts ?? new string[0]
              , endpoints ?? new string[0]
              , references ?? new string[0]
              , multipleAreas
              , embedStatements
              , errorReporter
            );
            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(context);
            detectedReferences = context.DetectedReferences.ToArray();
            return result;
        }
    }
}