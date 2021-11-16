using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    {
        public static bool Execute
        (
            string projectName
          , string projectDirectory
          , string productName
          , string areaName
          , string title
          , string version
          , string description
          , string baseUrl
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
          , out string[] additionalAssemblyReferences
        )
        {
            return Execute
            (
                projectName
              , projectDirectory
              , productName
              , areaName
              , title
              , version
              , description
              , baseUrl
              , defaultOutputFilePath
              , clientOutputFilePath
              , externalAssemblyReferenceDirectory
              , source
              , contracts
              , endpoints
              , references
              , defaultSecuritySchemes
              , isEmbedded
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , logger
              , sqlModel: null
              , out additionalAssemblyReferences
            );
        }
        internal static bool Execute
        (
            string projectName
          , string projectDirectory
          , string productName
          , string areaName
          , string title
          , string version
          , string description
          , string baseUrl
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
          , TSqlModel sqlModel
          , out string[] additionalAssemblyReferences
        )
        {
            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            CodeArtifactsGenerationModel codeGenerationModel = CodeGenerationModelLoader.Create
            (
                projectName
              , projectDirectory
              , productName
              , areaName
              , title
              , version
              , description
              , baseUrl
              , defaultOutputFilePath
              , clientOutputFilePath
              , externalAssemblyReferenceDirectory
              , source
              , contracts
              , endpoints
              , references
              , defaultSecuritySchemes
              , isEmbedded
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , schemaRegistry
              , logger
              , sqlModel
            );

            ICodeArtifactsGenerationModelValidator modelValidator = new CompositeCodeArtifactsGenerationModelValidator(new ContractArtifactValidator(logger));
            if (!modelValidator.Validate(codeGenerationModel))
            {
                additionalAssemblyReferences = null;
                return false;
            }

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(codeGenerationModel, schemaRegistry, logger);
            additionalAssemblyReferences = codeGenerationModel.AdditionalAssemblyReferences.ToArray();
            return result;
        }
    }
}