using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeGenerationModel Create
        (
            SqlCoreConfiguration globalConfiguration
          , ArtifactGenerationConfiguration artifactGenerationConfiguration
          , ISchemaRegistry schemaRegistry
          , IExternalSchemaResolver externalSchemaResolver
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , DefaultAssemblyResolver assemblyResolver
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , LockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            string rootNamespace = PathUtility.BuildRootNamespace(artifactGenerationConfiguration.ProductName, artifactGenerationConfiguration.AreaName);
            string className = artifactGenerationConfiguration.DefaultOutputName.Replace(".", String.Empty);

            CodeGenerationModel model = new CodeGenerationModel(globalConfiguration, artifactGenerationConfiguration, rootNamespace, className);

            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            
            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !globalConfiguration.IsEmbedded;
            ISqlStatementParser parser = new SqlStoredProcedureParser(requireExplicitMarkup);
            Lazy<TSqlModel> modelAccessor = sqlModel != null ? new Lazy<TSqlModel>(() => sqlModel) : new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(globalConfiguration, logger));
            ISqlStatementFormatter formatter = SelectSqlStatementFormatter(globalConfiguration.IsEmbedded);
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            IDictionary<string, SecurityScheme> securitySchemeMap = new Dictionary<string, SecurityScheme>();

            ISchemaProvider builtInSchemaProvider = new BuiltInSchemaProvider();
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(artifactGenerationConfiguration, fileSystemProvider, typeResolver, logger);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(globalConfiguration, artifactGenerationConfiguration, typeResolver, logger);
            //ISchemaProvider externalSchemaProvider = new ExternalSchemaProvider(assemblyResolver);
            schemaRegistry.ImportSchemas(builtInSchemaProvider, contractDefinitionProvider, userDefinedTypeProvider/*, externalSchemaProvider*/);

            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(artifactGenerationConfiguration, schemaRegistry, contractDefinitionProvider, externalSchemaResolver, assemblyResolver, assemblyResolver, logger), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, externalSchemaResolver, assemblyResolver, logger), 2);

            ISqlStatementDefinitionProvider sqlStatementDefinitionProvider = new SqlStatementDefinitionProvider(globalConfiguration, artifactGenerationConfiguration, analyzeAlways: true, rootNamespace, parser, formatter, typeResolver, schemaRegistry, schemaDefinitionResolver, logger, modelAccessor);
            IActionTargetDefinitionResolverFacade actionTargetResolver = new ActionTargetDefinitionResolverFacade(artifactGenerationConfiguration, className, sqlStatementDefinitionProvider, externalSchemaResolver, assemblyResolver, lockEntryManager, schemaDefinitionResolver, schemaRegistry, logger);
            IControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(artifactGenerationConfiguration, securitySchemeMap, actionTargetResolver, typeResolver, schemaDefinitionResolver, actionParameterSourceRegistry, actionParameterConverterRegistry, lockEntryManager, fileSystemProvider, logger);

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