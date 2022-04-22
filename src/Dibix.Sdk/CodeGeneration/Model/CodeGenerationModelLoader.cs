using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.Model;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeGenerationModel Create
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
          , ISchemaRegistry schemaRegistry
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IActionParameterConverterRegistry actionParameterConverterRegistry
          , LockEntryManager lockEntryManager
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string className = defaultOutputName.Replace(".", String.Empty);

            ICollection<string> normalizedSources = source.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts.Select(x => x.GetFullPath());
            IEnumerable<string> normalizedEndpoints = endpoints.Select(x => x.GetFullPath());
            ICollection<string> normalizedReferences = references.Select(x => x.GetFullPath()).ToArray();
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

            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, externalAssemblyReferenceDirectory, normalizedReferences);
            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            
            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !isEmbedded;
            ISqlStatementParser parser = new SqlStoredProcedureParser(requireExplicitMarkup);
            Lazy<TSqlModel> modelAccessor = sqlModel != null ? new Lazy<TSqlModel>(() => sqlModel) : new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger));
            ISqlStatementFormatter formatter = isEmbedded ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            IDictionary<string, SecurityScheme> securitySchemeMap = new Dictionary<string, SecurityScheme>();

            ISchemaProvider builtInSchemaProvider = new BuiltInSchemaProvider();
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, logger, normalizedContracts, rootNamespace, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(rootNamespace, productName, areaName, normalizedSources, typeResolver, logger);
            //ISchemaProvider externalSchemaProvider = new ExternalSchemaProvider(assemblyResolver);
            schemaRegistry.ImportSchemas(builtInSchemaProvider, contractDefinitionProvider, userDefinedTypeProvider/*, externalSchemaProvider*/);

            IExternalSchemaResolver externalSchemaResolver = new ExternalSchemaResolver(assemblyResolver, schemaRegistry);
            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider, externalSchemaResolver, assemblyResolver, assemblyResolver, logger, rootNamespace, productName, areaName), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, externalSchemaResolver, assemblyResolver, logger), 2);

            ISqlStatementDefinitionProvider sqlStatementDefinitionProvider = new SqlStatementDefinitionProvider(projectName, isEmbedded, analyzeAlways: true, rootNamespace, productName, areaName, parser, formatter, typeResolver, schemaRegistry, logger, normalizedSources, modelAccessor);
            IActionDefinitionResolverFacade actionResolver = new ActionDefinitionResolverFacade(projectName, rootNamespace, productName, areaName, className, sqlStatementDefinitionProvider, externalSchemaResolver, assemblyResolver, lockEntryManager, schemaRegistry, logger);
            IControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(normalizedEndpoints, normalizedDefaultSecuritySchemes, securitySchemeMap, actionResolver, typeResolver, schemaRegistry, actionParameterSourceRegistry, actionParameterConverterRegistry, lockEntryManager, fileSystemProvider, logger);

            schemaRegistry.ImportSchemas(sqlStatementDefinitionProvider);

            model.SqlStatements.AddRange(sqlStatementDefinitionProvider.SqlStatements);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(controllerDefinitionProvider.Controllers);
            model.SecuritySchemes.AddRange(securitySchemeMap.Values);
            model.Schemas.AddRange(schemaRegistry.Schemas);

            return model;
        }
    }
}