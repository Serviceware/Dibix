using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    {
        internal static bool Execute
        (
            SqlCoreConfiguration globalConfiguration
          , ArtifactGenerationConfiguration artifactGenerationConfiguration
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , LockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
          , ICollection<string> additionalAssemblyReferences
        )
        {
            logger.LogMessage("Generating code artifacts...");

            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(globalConfiguration, artifactGenerationConfiguration);
            ExternalSchemaResolver externalSchemaResolver = new ExternalSchemaResolver(assemblyResolver, schemaRegistry);
            SchemaDefinitionResolver schemaDefinitionResolver = new SchemaDefinitionResolver(schemaRegistry, externalSchemaResolver, logger);
            CodeGenerationModel codeGenerationModel = CodeGenerationModelLoader.Create
            (
                globalConfiguration
              , artifactGenerationConfiguration
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
                return false;
            }

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(codeGenerationModel, schemaDefinitionResolver, logger);

            additionalAssemblyReferences.AddRange(codeGenerationModel.AdditionalAssemblyReferences);
            return result;
        }
    }
}