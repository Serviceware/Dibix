using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationTask
    {
        public static bool Execute
        (
            CodeGenerationConfiguration configuration
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , ILockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
          , ICollection<string> additionalAssemblyReferences
        )
        {
            logger.LogMessage("Generating code artifacts...");

            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(configuration.ProjectDirectory, configuration.ExternalAssemblyReferenceDirectory, configuration.References);
            ExternalSchemaResolver externalSchemaResolver = new ExternalSchemaResolver(assemblyResolver, schemaRegistry);
            SchemaDefinitionResolver schemaDefinitionResolver = new SchemaDefinitionResolver(schemaRegistry, externalSchemaResolver, logger);
            CodeGenerationModel model = CodeGenerationModelLoader.Load
            (
                configuration
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

            if (!modelValidator.Validate(model))
            {
                return false;
            }

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(model, schemaDefinitionResolver, logger);

            additionalAssemblyReferences.AddRange(model.AdditionalAssemblyReferences);
            return result;
        }
    }
}