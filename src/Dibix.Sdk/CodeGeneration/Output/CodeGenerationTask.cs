using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    {
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
          , bool limitDdlStatements
          , bool preventDmlReferences
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
              , limitDdlStatements
              , preventDmlReferences
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