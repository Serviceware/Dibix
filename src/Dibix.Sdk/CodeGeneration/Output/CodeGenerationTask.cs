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
          , string outputName
          , string title
          , string version
          , string description
          , string baseUrl
          , string defaultOutputFilePath
          , string endpointOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , bool enableExperimentalFeatures
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
          , out string[] additionalAssemblyReferences
        )
        {
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();
            IActionParameterConverterRegistry actionParameterConverterRegistry = new ActionParameterConverterRegistry();
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            using (LockEntryManager lockEntryManager = LockEntryManager.Create())
            {
                return Execute
                (
                    projectName
                  , projectDirectory
                  , productName
                  , areaName
                  , outputName
                  , title
                  , version
                  , description
                  , new EndpointConfiguration(baseUrl)
                  , defaultOutputFilePath
                  , endpointOutputFilePath
                  , clientOutputFilePath
                  , externalAssemblyReferenceDirectory
                  , source
                  , contracts
                  , endpoints
                  , references
                  , defaultSecuritySchemes
                  , isEmbedded
                  , enableExperimentalFeatures
                  , databaseSchemaProviderName
                  , modelCollation
                  , sqlReferencePath
                  , actionParameterSourceRegistry
                  , actionParameterConverterRegistry
                  , lockEntryManager
                  , fileSystemProvider
                  , logger
                  , sqlModel: null
                  , out additionalAssemblyReferences
                );
            }
        }
        internal static bool Execute
        (
            string projectName
          , string projectDirectory
          , string productName
          , string areaName
          , string outputName
          , string title
          , string version
          , string description
          , EndpointConfiguration endpointConfiguration
          , string defaultOutputFilePath
          , string endpointOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , bool enableExperimentalFeatures
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , LockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
          , out string[] additionalAssemblyReferences
        )
        {
            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            CodeGenerationModel codeGenerationModel = CodeGenerationModelLoader.Create
            (
                projectName
              , projectDirectory
              , productName
              , areaName
              , outputName
              , title
              , version
              , description
              , endpointConfiguration
              , defaultOutputFilePath
              , endpointOutputFilePath
              , clientOutputFilePath
              , externalAssemblyReferenceDirectory
              , source
              , contracts
              , endpoints
              , references
              , defaultSecuritySchemes
              , isEmbedded
              , enableExperimentalFeatures
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , schemaRegistry
              , actionParameterSourceRegistry
              , actionParameterConverterRegistry
              , lockEntryManager
              , fileSystemProvider
              , logger
              , sqlModel
            );

            ICodeGenerationModelValidator modelValidator = new CompositeCodeGenerationModelValidator
            (
                new ContractArtifactModelValidator(logger)
              , new ActionParameterPropertySourceModelValidator(actionParameterSourceRegistry, schemaRegistry, logger)
            );
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