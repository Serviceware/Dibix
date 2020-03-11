using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.CodeGeneration.MSBuild;
using Microsoft.Build.Framework;
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
          , ITaskItem[] source
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , IEnumerable<string> references
          , bool embedStatements
          , string databaseSchemaProviderName
          , string modelCollation
          , ITaskItem[] sqlReferencePath
          , ITask task
          , TaskLoggingHelper logger
          , out string[] additionalAssemblyReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ISchemaRegistry schemaRegistry = new SchemaRegistry(errorReporter);
            CodeArtifactsGenerationModel model = MSBuildCodeGenerationModelLoader.Create
            (
                projectDirectory
              , productName
              , areaName
              , defaultOutputFilePath
              , clientOutputFilePath
              , source
              , contracts
              , endpoints
              , references
              , embedStatements
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , task
              , schemaRegistry
              , errorReporter
            );

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(model, schemaRegistry, errorReporter);
            additionalAssemblyReferences = model.AdditionalAssemblyReferences.ToArray();
            return result;
        }
    }
}