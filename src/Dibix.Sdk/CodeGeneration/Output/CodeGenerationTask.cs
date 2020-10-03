using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string productName
          , string areaName
          , string title
          , string version
          , string description
          , string baseUrl
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , bool embedStatements
          , string databaseSchemaProviderName
          , string modelCollation
          , IEnumerable<TaskItem> sqlReferencePath
          , ILogger logger
          , out string[] additionalAssemblyReferences
        )
        {
            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            CodeArtifactsGenerationModel model = CodeGenerationModelLoader.Create
            (
                projectDirectory
              , productName
              , areaName
              , title
              , version
              , description
              , baseUrl
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
              , schemaRegistry
              , logger
            );

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(model, schemaRegistry, logger);
            additionalAssemblyReferences = model.AdditionalAssemblyReferences.ToArray();
            return result;
        }
    }
}