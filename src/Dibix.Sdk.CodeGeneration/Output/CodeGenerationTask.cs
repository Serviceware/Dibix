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
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            logger.LogMessage("Generating code artifacts...");

            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            CodeGenerationModel model = CodeGenerationModelLoader.Load
            (
                configuration
              , securitySchemes
              , schemaRegistry
              , actionParameterSourceRegistry
              , actionParameterConverterRegistry
              , lockEntryManager
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