using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeGenerationModel Create
        (
            string projectName
          , string productName
          , string areaName
          , string title
          , string version
          , string description
          , EndpointConfiguration endpointConfiguration
          , string outputDirectory
          , string defaultOutputName
          , string clientOutputName
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , bool limitDdlStatements
          , bool preventDmlReferences
          , bool enableExperimentalFeatures
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
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
            string rootNamespace = PathUtility.BuildRootNamespace(productName, areaName);
            string className = defaultOutputName.Replace(".", String.Empty);

            ICollection<string> normalizedSources = source.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts.Select(x => x.GetFullPath());
            IEnumerable<string> normalizedEndpoints = endpoints.Select(x => x.GetFullPath());
            ICollection<string> normalizedDefaultSecuritySchemes = defaultSecuritySchemes.Select(x => x.ItemSpec).ToArray();

            CodeGenerationModel model = new CodeGenerationModel
            {
                ProductName = productName,
                AreaName = areaName,
                Title = title,
                Version = version,
                Description = description,
                EndpointConfiguration = endpointConfiguration,
                RootNamespace = rootNamespace,
                DefaultClassName = className,
                OutputDirectory = outputDirectory,
                DefaultOutputName = defaultOutputName,
                ClientOutputName = clientOutputName,
                EnableExperimentalFeatures = enableExperimentalFeatures
            };

            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            
            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !isEmbedded;
            ISqlStatementParser parser = new SqlStoredProcedureParser(requireExplicitMarkup);
            Lazy<TSqlModel> modelAccessor = sqlModel != null ? new Lazy<TSqlModel>(() => sqlModel) : new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(preventDmlReferences, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger));
            ISqlStatementFormatter formatter = SelectSqlStatementFormatter(isEmbedded);
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            IDictionary<string, SecurityScheme> securitySchemeMap = new Dictionary<string, SecurityScheme>();

            ISchemaProvider builtInSchemaProvider = new BuiltInSchemaProvider();
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, typeResolver, logger, normalizedContracts, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(rootNamespace, productName, areaName, normalizedSources, typeResolver, logger);
            //ISchemaProvider externalSchemaProvider = new ExternalSchemaProvider(assemblyResolver);
            schemaRegistry.ImportSchemas(builtInSchemaProvider, contractDefinitionProvider, userDefinedTypeProvider/*, externalSchemaProvider*/);

            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider, externalSchemaResolver, assemblyResolver, assemblyResolver, logger, productName, areaName), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, externalSchemaResolver, assemblyResolver, logger), 2);

            ISqlStatementDefinitionProvider sqlStatementDefinitionProvider = new SqlStatementDefinitionProvider(projectName, isEmbedded, limitDdlStatements, analyzeAlways: true, rootNamespace, productName, areaName, parser, formatter, typeResolver, schemaRegistry, schemaDefinitionResolver, logger, normalizedSources, modelAccessor);
            IActionDefinitionResolverFacade actionResolver = new ActionDefinitionResolverFacade(productName, areaName, className, sqlStatementDefinitionProvider, externalSchemaResolver, assemblyResolver, lockEntryManager, schemaDefinitionResolver, schemaRegistry, logger);
            IControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(normalizedEndpoints, normalizedDefaultSecuritySchemes, securitySchemeMap, actionResolver, typeResolver, schemaDefinitionResolver, actionParameterSourceRegistry, actionParameterConverterRegistry, lockEntryManager, fileSystemProvider, logger);

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