using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.CodeGeneration.MSBuild;
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
          , bool embedStatements
          , TaskLoggingHelper logger
          , out string[] additionalAssemblyReferences
        )
        {
            return ExecuteCore
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
              , embedStatements
              , logger
              , out additionalAssemblyReferences
            );
        }

        private static bool ExecuteCore
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
          , TaskLoggingHelper logger
          , out string[] additionalAssemblyReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            CodeArtifactsGenerationModel model = MSBuildCodeGenerationModelLoader.Create
            (
                projectDirectory
              , productName
              , areaName
              , defaultOutputFilePath
              , clientOutputFilePath
              , sources
              , contracts
              , endpoints
              , references
              , embedStatements
              , errorReporter
            );

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(model, errorReporter);
            additionalAssemblyReferences = model.AdditionalAssemblyReferences.ToArray();
            return result;
        }
    }
}