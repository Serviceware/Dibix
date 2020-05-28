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
            string projectDirectory
          , string projectVersion
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , bool embedStatements
          , string databaseSchemaProviderName
          , string modelCollation
          , IEnumerable<TaskItem> sqlReferencePath
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string defaultOutputName = defaultOutputFilePath != null ? Path.GetFileNameWithoutExtension(defaultOutputFilePath).Replace(".", String.Empty) : "SqlQueryAccessor";

            ICollection<string> normalizedSources = source.Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts.Select(x => x.GetFullPath());
            IEnumerable<string> normalizedEndpoints = endpoints.Select(x => x.GetFullPath());
            ICollection<string> normalizedReferences = references.Select(x => x.GetFullPath()).ToArray();

            Lazy<TSqlModel> modelAccessor = new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger));
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            ISqlStatementFormatter formatter = embedStatements ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, normalizedReferences);
            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, logger);
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, logger, normalizedContracts, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(productName, areaName, normalizedSources, typeResolver, logger);

            schemaRegistry.ImportSchemas(contractDefinitionProvider, userDefinedTypeProvider);
            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider), 0);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, assemblyResolver), 1);

            CodeArtifactsGenerationModel model = new CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel.Full)
            {
                ProjectVersion = projectVersion,
                AreaName = areaName,
                RootNamespace = rootNamespace,
                DefaultClassName = defaultOutputName,
                ProductName = productName,
                DefaultOutputFilePath = defaultOutputFilePath,
                ClientOutputFilePath = clientOutputFilePath
            };
            model.Statements.AddRange(CollectStatements(normalizedSources, projectDirectory, productName, areaName, embedStatements, formatter, typeResolver, schemaRegistry, logger, modelAccessor));
            model.UserDefinedTypes.AddRange(userDefinedTypeProvider.Types);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(CollectControllers(normalizedEndpoints, productName, areaName, defaultOutputName, model.Statements, assemblyResolver, fileSystemProvider, typeResolver, schemaRegistry, logger));

            return model;
        }

        private static IEnumerable<SqlStatementInfo> CollectStatements
        (
            IEnumerable<string> sources
          , string projectDirectory
          , string productName
          , string areaName
          , bool embedStatements
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , Lazy<TSqlModel> modelAccessor)
        {
            IEnumerable<string> files = sources.Where(x => MatchStatement(projectDirectory, x, embedStatements, logger));
            ISqlStatementParser parser = new SqlStoredProcedureParser();
            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector(productName, areaName, parser, formatter, typeResolver, schemaRegistry, logger, files, modelAccessor);
            return statementCollector.CollectStatements();
        }

        private static bool MatchStatement(string projectDirectory, string relativeFilePath, bool embedStatements, ILogger logger)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            ICollection<SqlHint> hints = SqlHintParser.FromFile(inputFilePath, logger).ToArray();
            bool hasHints = hints.Any();
            bool hasNoCompileHint = hints.Any(x => x.Kind == SqlHint.NoCompile);
            return (embedStatements || hasHints) && !hasNoCompileHint;
        }

        private static IEnumerable<ControllerDefinition> CollectControllers
        (
            IEnumerable<string> endpoints
          , string productName
          , string areaName
          , string defaultOutputName
          , ICollection<SqlStatementInfo> statements
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , IFileSystemProvider fileSystemProvider
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            IControllerActionTargetSelector controllerActionTargetSelector = new ControllerActionTargetSelector(productName, areaName, defaultOutputName, statements, referencedAssemblyInspector, schemaRegistry, logger);
            return new ControllerDefinitionProvider(fileSystemProvider, controllerActionTargetSelector, typeResolver, logger, endpoints).Controllers;
        }
    }
}