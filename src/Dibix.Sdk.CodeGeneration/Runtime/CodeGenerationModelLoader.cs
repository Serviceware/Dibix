using System;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeGenerationModel Load
        (
            CodeGenerationConfiguration configuration
          , SecuritySchemes securitySchemes
          , ISchemaRegistry schemaRegistry
          , DefaultAssemblyResolver assemblyResolver
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , ILockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            string rootNamespace = PathUtility.BuildRootNamespace(configuration.ProductName, configuration.AreaName);
            string defaultClassName = configuration.DefaultOutputName.Replace(".", String.Empty);

            CodeGenerationModel model = new CodeGenerationModel
            {
                AreaName = configuration.AreaName,
                RootNamespace = rootNamespace,
                DefaultClassName = defaultClassName,
                Title = configuration.Title,
                Version = configuration.Version,
                Description = configuration.Description,
                OutputDirectory = configuration.OutputDirectory,
                DefaultOutputName = configuration.DefaultOutputName,
                ClientOutputName = configuration.ClientOutputName,
                BaseUrl = configuration.BaseUrl,
                EnableExperimentalFeatures = configuration.EnableExperimentalFeatures,
                UseMicrosoftHttpClient = configuration.UseMicrosoftHttpClient
            };

            ITypeResolverFacade typeResolver = new TypeResolverFacade(logger);
            typeResolver.Register(new SchemaTypeResolver(configuration.ProductName, configuration.AreaName, schemaRegistry), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry), 2);

            ISqlStatementParser parser = new SqlStoredProcedureParser(configuration.IsEmbedded);
            ISqlStatementFormatter formatter = SelectSqlStatementFormatter(configuration.IsEmbedded);
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;

            schemaRegistry.ImportSchemas
            (
                new BuiltInSchemaProvider()
              , new ExternalSchemaProvider(assemblyResolver)
              , new ContractDefinitionSchemaProvider(configuration.ProductName, configuration.AreaName, configuration.Contracts, fileSystemProvider, typeResolver, schemaRegistry, logger)
              , new UserDefinedTypeSchemaProvider(configuration.ProductName, configuration.AreaName, configuration.Source, typeResolver, logger)
              , new SqlStatementEnumSchemaProvider(configuration.ProductName, configuration.AreaName, configuration.Source, logger)
              , new SqlStatementDefinitionProvider(configuration.IsEmbedded, configuration.LimitDdlStatements, analyzeAlways: true, configuration.ProductName, configuration.AreaName, configuration.Source, parser, formatter, typeResolver, schemaRegistry, logger, sqlModel)
            );

            IActionTargetDefinitionResolverFacade actionTargetResolver = new ActionTargetDefinitionResolverFacade(configuration.ProductName, configuration.AreaName, defaultClassName, lockEntryManager, schemaRegistry, logger);
            IControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(configuration.Endpoints, securitySchemes, configuration.ConfigurationTemplates, actionTargetResolver, typeResolver, schemaRegistry, actionParameterSourceRegistry, actionParameterConverterRegistry, lockEntryManager, fileSystemProvider, logger);

            model.Controllers.AddRange(controllerDefinitionProvider.Controllers);
            model.SecuritySchemes.AddRange(controllerDefinitionProvider.SecuritySchemes);
            model.Schemas.AddRange(schemaRegistry.Schemas);

            return model;
        }

        private static ISqlStatementFormatter SelectSqlStatementFormatter(bool isEmbedded)
        {
            if (isEmbedded)
                return new TakeSourceSqlStatementFormatter();
            
            return new ExecStoredProcedureSqlStatementFormatter();
        }
    }
}