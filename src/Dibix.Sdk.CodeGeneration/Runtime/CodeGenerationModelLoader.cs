using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeGenerationModel Load
        (
            CodeGenerationConfiguration configuration
          , ISchemaRegistry schemaRegistry
          , IExternalSchemaResolver externalSchemaResolver
          , ISchemaDefinitionResolver schemaDefinitionResolver
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
                EnableExperimentalFeatures = configuration.EnableExperimentalFeatures
            };

            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            
            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !configuration.IsEmbedded;
            ISqlStatementParser parser = new SqlStoredProcedureParser(requireExplicitMarkup);
            ISqlStatementFormatter formatter = SelectSqlStatementFormatter(configuration.IsEmbedded);
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            IDictionary<string, SecurityScheme> securitySchemeMap = new Dictionary<string, SecurityScheme>();

            ISchemaProvider builtInSchemaProvider = new BuiltInSchemaProvider();
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(configuration.ProductName, configuration.AreaName, configuration.Contracts, fileSystemProvider, typeResolver, schemaDefinitionResolver, logger);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(configuration.ProductName, configuration.AreaName, configuration.Source, typeResolver, logger);
            //ISchemaProvider externalSchemaProvider = new ExternalSchemaProvider(assemblyResolver);
            schemaRegistry.ImportSchemas(builtInSchemaProvider, contractDefinitionProvider, userDefinedTypeProvider/*, externalSchemaProvider*/);

            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(configuration.ProductName, configuration.AreaName, schemaRegistry, contractDefinitionProvider, externalSchemaResolver, assemblyResolver, assemblyResolver, logger), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, externalSchemaResolver, assemblyResolver, logger), 2);

            ISqlStatementDefinitionProvider sqlStatementDefinitionProvider = new SqlStatementDefinitionProvider(configuration.IsEmbedded, configuration.LimitDdlStatements, analyzeAlways: true, configuration.ProductName, configuration.AreaName, configuration.Source, parser, formatter, typeResolver, schemaRegistry, schemaDefinitionResolver, logger, sqlModel);
            IActionTargetDefinitionResolverFacade actionTargetResolver = new ActionTargetDefinitionResolverFacade(configuration.ProductName, configuration.AreaName, defaultClassName, sqlStatementDefinitionProvider, externalSchemaResolver, assemblyResolver, lockEntryManager, schemaDefinitionResolver, schemaRegistry, logger);
            IControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(configuration.Endpoints, configuration.DefaultSecuritySchemes, securitySchemeMap, actionTargetResolver, typeResolver, schemaDefinitionResolver, actionParameterSourceRegistry, actionParameterConverterRegistry, lockEntryManager, fileSystemProvider, logger);

            schemaRegistry.ImportSchemas(sqlStatementDefinitionProvider);

            model.SqlStatements.AddRange(sqlStatementDefinitionProvider.SqlStatements);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(controllerDefinitionProvider.Controllers);
            model.SecuritySchemes.AddRange(securitySchemeMap.Values);
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