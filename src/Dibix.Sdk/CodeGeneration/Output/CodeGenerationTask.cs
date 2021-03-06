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
          , string outputDirectory
          , string defaultOutputName
          , string clientOutputName
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
                  , title
                  , version
                  , description
                  , new EndpointConfiguration(baseUrl)
                  , outputDirectory
                  , defaultOutputName
                  , clientOutputName
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
          , string title
          , string version
          , string description
          , EndpointConfiguration endpointConfiguration
          , string outputDirectory
          , string defaultOutputName
          , string clientOutputName
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
            logger.LogMessage("Generating code artifacts...");

            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            ICollection<string> normalizedReferences = references.Select(x => x.GetFullPath()).ToArray();
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, externalAssemblyReferenceDirectory, normalizedReferences);
            ExternalSchemaResolver externalSchemaResolver = new ExternalSchemaResolver(assemblyResolver, schemaRegistry);
            SchemaDefinitionResolver schemaDefinitionResolver = new SchemaDefinitionResolver(schemaRegistry, externalSchemaResolver, logger);
            CodeGenerationModel codeGenerationModel = CodeGenerationModelLoader.Create
            (
                projectName, productName
              , areaName
              , title
              , version
              , description
              , endpointConfiguration
              , outputDirectory
              , defaultOutputName
              , clientOutputName, source
              , contracts
              , endpoints, defaultSecuritySchemes
              , isEmbedded
              , enableExperimentalFeatures
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , schemaRegistry
              , externalSchemaResolver
              , schemaDefinitionResolver
              , assemblyResolver
              , actionParameterSourceRegistry
              , actionParameterConverterRegistry
              , lockEntryManager
              , fileSystemProvider
              , logger
              , sqlModel
            );

            ICodeGenerationModelValidator modelValidator = new CompositeCodeGenerationModelValidator
            (
                new ActionParameterPropertySourceModelValidator(actionParameterSourceRegistry, schemaDefinitionResolver, logger)
              , new ContractArtifactModelValidator(schemaDefinitionResolver, logger)
              , new EndpointModelValidator(logger)
              , new UserDefinedTypeParameterModelValidator(schemaDefinitionResolver, logger)
            );

            if (!modelValidator.Validate(codeGenerationModel))
            {
                additionalAssemblyReferences = null;
                return false;
            }

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(codeGenerationModel, schemaDefinitionResolver, logger);
            additionalAssemblyReferences = codeGenerationModel.AdditionalAssemblyReferences.ToArray();
            return result;
        }
    }
}