using System;
using System.Collections.Generic;
using System.IO;
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
          , string title
          , string version
          , string description
          , string baseUrl
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> securitySchemes
          , bool isEmbedded
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , TSqlModel sqlModel
        )
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string defaultOutputName = defaultOutputFilePath != null ? Path.GetFileNameWithoutExtension(defaultOutputFilePath).Replace(".", String.Empty) : "SqlQueryAccessor";

            ICollection<string> normalizedSources = source.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts.Select(x => x.GetFullPath());
            IEnumerable<string> normalizedEndpoints = endpoints.Select(x => x.GetFullPath());
            ICollection<string> normalizedReferences = references.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<SecurityScheme> normalizedSecuritySchemes = securitySchemes.Select(x => new SecurityScheme(x.ItemSpec));

            CodeArtifactsGenerationModel model = new CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel.Full)
            {
                ProductName = productName,
                AreaName = areaName,
                Title = title,
                Version = version,
                Description = description,
                BaseUrl = baseUrl,
                RootNamespace = rootNamespace,
                DefaultClassName = defaultOutputName,
                DefaultOutputFilePath = defaultOutputFilePath,
                ClientOutputFilePath = clientOutputFilePath
            };

            Lazy<TSqlModel> modelAccessor = sqlModel != null ? new Lazy<TSqlModel>(() => sqlModel) : new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger));
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            ISqlStatementFormatter formatter = isEmbedded ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, externalAssemblyReferenceDirectory, normalizedReferences);
            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, logger, normalizedContracts, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(productName, areaName, normalizedSources, typeResolver, logger);

            schemaRegistry.ImportSchemas(contractDefinitionProvider, userDefinedTypeProvider);
            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider), 0);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, assemblyResolver), 1);

            model.Statements.AddRange(CollectStatements(normalizedSources, projectName, productName, areaName, isEmbedded, formatter, typeResolver, schemaRegistry, logger, modelAccessor));
            model.UserDefinedTypes.AddRange(userDefinedTypeProvider.Types);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(CollectControllers(normalizedEndpoints, projectName, productName, areaName, defaultOutputName, model.Statements, normalizedSecuritySchemes, assemblyResolver, fileSystemProvider, typeResolver, schemaRegistry, logger));

            return model;
        }

        private static IEnumerable<SqlStatementInfo> CollectStatements
        (
            IEnumerable<string> sources
          , string projectName
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
            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector(projectName, isEmbedded, analyzeAlways: true, productName, areaName, parser, formatter, typeResolver, schemaRegistry, logger, sources, modelAccessor);
            return statementCollector.CollectStatements();
        }

        private static IEnumerable<ControllerDefinition> CollectControllers
        (
            IEnumerable<string> endpoints
          , string projectName
          , string productName
          , string areaName
          , string defaultOutputName
          , ICollection<SqlStatementInfo> statements
          , IEnumerable<SecurityScheme> securitySchemes
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , IFileSystemProvider fileSystemProvider
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            ControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(projectName, productName, areaName, defaultOutputName, statements, endpoints, securitySchemes, typeResolver, referencedAssemblyInspector, schemaRegistry, fileSystemProvider, logger);
            return controllerDefinitionProvider.Controllers;
        }
    }
}