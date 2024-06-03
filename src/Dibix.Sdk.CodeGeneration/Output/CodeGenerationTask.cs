using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    {
        public static bool Execute
        (
            CodeGenerationConfiguration configuration
          , SecuritySchemes securitySchemes
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , ILockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            logger.LogMessage("Generating code artifacts...");

            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(configuration.ProjectDirectory, configuration.ExternalAssemblyReferenceDirectory, configuration.References);
            CodeGenerationModel model = CodeGenerationModelLoader.Load
            (
                configuration
              , securitySchemes
              , schemaRegistry
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
                new ActionParameterPropertySourceModelValidator(actionParameterSourceRegistry, schemaRegistry, logger)
              , new ContractArtifactModelValidator(schemaRegistry, logger)
              , new EndpointModelValidator(schemaRegistry, logger)
              , new UserDefinedTypeParameterModelValidator(schemaRegistry, logger)
            );

            if (!modelValidator.Validate(model))
            {
                return false;
            }

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(model, schemaRegistry, logger);
            
            return result;
        }
    }
}