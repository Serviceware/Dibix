using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelLoader
    {
        public static CodeArtifactsGenerationModel Create
        (
            string projectName
          , string projectDirectory
          , string productName
          , string areaName
          , string outputName
          , string title
          , string version
          , string description
          , EndpointConfiguration endpointConfiguration
          , string defaultOutputFilePath
          , string endpointOutputFilePath
          , string clientOutputFilePath
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
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string className = outputName.Replace(".", String.Empty);

            ICollection<string> normalizedSources = source.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts.Select(x => x.GetFullPath());
            IEnumerable<string> normalizedEndpoints = endpoints.Select(x => x.GetFullPath());
            ICollection<string> normalizedReferences = references.Select(x => x.GetFullPath()).ToArray();
            ICollection<string> normalizedDefaultSecuritySchemes = defaultSecuritySchemes.Select(x => x.ItemSpec).ToArray();

            CodeArtifactsGenerationModel model = new CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel.Full)
            {
                ProductName = productName,
                AreaName = areaName,
                Title = title,
                Version = version,
                Description = description,
                EndpointConfiguration = endpointConfiguration,
                RootNamespace = rootNamespace,
                DefaultClassName = className,
                DefaultOutputFilePath = defaultOutputFilePath,
                EndpointOutputFilePath = endpointOutputFilePath,
                ClientOutputFilePath = clientOutputFilePath,
                EnableExperimentalFeatures = enableExperimentalFeatures
            };

            Lazy<TSqlModel> modelAccessor = sqlModel != null ? new Lazy<TSqlModel>(() => sqlModel) : new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger));
            ISqlStatementFormatter formatter = isEmbedded ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, externalAssemblyReferenceDirectory, normalizedReferences);
            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, logger, normalizedContracts, rootNamespace, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(rootNamespace, productName, areaName, normalizedSources, typeResolver, logger);

            schemaRegistry.ImportSchemas(contractDefinitionProvider, userDefinedTypeProvider);
            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider, assemblyResolver, logger, rootNamespace, productName, areaName), 1);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, assemblyResolver, logger), 2);

            IDictionary<string, SecurityScheme> securitySchemeMap = new Dictionary<string, SecurityScheme>();

            model.Statements.AddRange(CollectStatements(normalizedSources, projectName, rootNamespace, productName, areaName, isEmbedded, formatter, typeResolver, schemaRegistry, logger, modelAccessor));
            model.UserDefinedTypes.AddRange(userDefinedTypeProvider.Types);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(CollectControllers(normalizedEndpoints, projectName, rootNamespace, productName, areaName, className, endpointConfiguration, model.Statements, normalizedDefaultSecuritySchemes, securitySchemeMap, assemblyResolver, typeResolver, schemaRegistry, actionParameterSourceRegistry, fileSystemProvider, logger));
            model.SecuritySchemes.AddRange(securitySchemeMap.Values);
            model.Schemas.AddRange(schemaRegistry.Schemas);

            return model;
        }

        private static IEnumerable<SqlStatementDescriptor> CollectStatements
        (
            IEnumerable<string> sources
          , string projectName
          , string rootNamespace
          , string productName
          , string areaName
          , bool isEmbedded
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , Lazy<TSqlModel> modelAccessor)
        {
            // Currently only DML statements are included automatically
            // DDL statements however, need explicit markup, i.E. @Name at least
            bool requireExplicitMarkup = !isEmbedded;
            ISqlStatementParser parser = new SqlStoredProcedureParser(requireExplicitMarkup);
            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector(projectName, isEmbedded, analyzeAlways: true, rootNamespace, productName, areaName, parser, formatter, typeResolver, schemaRegistry, logger, sources, modelAccessor);
            return statementCollector.CollectStatements();
        }

        private static IEnumerable<ControllerDefinition> CollectControllers
        (
            IEnumerable<string> endpoints
          , string projectName
          , string rootNamespace
          , string productName
          , string areaName
          , string className
          , EndpointConfiguration endpointConfiguration
          , ICollection<SqlStatementDescriptor> statements
          , ICollection<string> defaultSecuritySchemes
          , IDictionary<string, SecurityScheme> securitySchemeMap
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , IActionParameterSourceRegistry actionParameterSourceRegistry
          , IFileSystemProvider fileSystemProvider
          , ILogger logger
        )
        {
            ControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(projectName, rootNamespace, productName, areaName, className, endpointConfiguration, statements, endpoints, defaultSecuritySchemes, securitySchemeMap, typeResolver, referencedAssemblyInspector, schemaRegistry, actionParameterSourceRegistry, fileSystemProvider, logger);
            return controllerDefinitionProvider.Controllers;
        }
    }
}