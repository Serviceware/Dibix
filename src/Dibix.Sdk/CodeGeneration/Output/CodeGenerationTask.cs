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
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> source
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , IEnumerable<string> references
          , bool embedStatements
          , string databaseSchemaProviderName
          , string modelCollation
          , IEnumerable<string> sqlReferencePath
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