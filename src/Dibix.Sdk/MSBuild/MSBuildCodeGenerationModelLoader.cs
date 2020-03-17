using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.Build.Framework;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class MSBuildCodeGenerationModelLoader
    {
        public static CodeArtifactsGenerationModel Create
        (
            string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ITaskItem[] source
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , IEnumerable<string> references
          , bool embedStatements
          , string databaseSchemaProviderName
          , string modelCollation
          , ITaskItem[] sqlReferencePath
          , ITask task
          , ISchemaRegistry schemaRegistry
          , IErrorReporter errorReporter
        )
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string defaultOutputName = defaultOutputFilePath != null ? Path.GetFileNameWithoutExtension(defaultOutputFilePath).Replace(".", String.Empty) : "SqlQueryAccessor";

            ICollection<string> normalizedSources = (source ?? Enumerable.Empty<ITaskItem>()).Select(x => x.GetFullPath()).ToArray();
            IEnumerable<string> normalizedContracts = contracts ?? Enumerable.Empty<string>();
            IEnumerable<string> normalizedEndpoints = endpoints ?? Enumerable.Empty<string>();
            ICollection<string> normalizedReferences = (references ?? Enumerable.Empty<string>()).ToArray();

            Lazy<TSqlModel> modelAccessor = new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, task, errorReporter));
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            ISqlStatementFormatter formatter = embedStatements ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver(projectDirectory, normalizedReferences);
            ITypeResolverFacade typeResolver = new TypeResolverFacade(assemblyResolver, schemaRegistry, errorReporter);
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, errorReporter, normalizedContracts, productName, areaName);
            IUserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(productName, areaName, normalizedSources, typeResolver, errorReporter);

            schemaRegistry.ImportSchemas(contractDefinitionProvider, userDefinedTypeProvider);
            typeResolver.Register(new ContractDefinitionSchemaTypeResolver(schemaRegistry, contractDefinitionProvider), 0);
            typeResolver.Register(new UserDefinedTypeSchemaTypeResolver(schemaRegistry, userDefinedTypeProvider, assemblyResolver), 1);

            CodeArtifactsGenerationModel model = new CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel.Full)
            {
                AreaName = areaName,
                RootNamespace = rootNamespace,
                DefaultClassName = defaultOutputName,
                ProductName = productName,
                DefaultOutputFilePath = defaultOutputFilePath,
                ClientOutputFilePath = clientOutputFilePath
            };
            model.Statements.AddRange(CollectStatements(normalizedSources, projectDirectory, productName, areaName, embedStatements, formatter, typeResolver, schemaRegistry, errorReporter, modelAccessor));
            model.UserDefinedTypes.AddRange(userDefinedTypeProvider.Types);
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(CollectControllers(normalizedEndpoints, productName, areaName, defaultOutputName, model.Statements, assemblyResolver, fileSystemProvider, typeResolver, errorReporter));

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
          , IErrorReporter errorReporter
          , Lazy<TSqlModel> modelAccessor
        )
        {
            IEnumerable<string> files = sources.Where(x => MatchStatement(projectDirectory, x, embedStatements, errorReporter));
            ISqlStatementParser parser = new SqlStoredProcedureParser();
            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector(productName, areaName, parser, formatter, typeResolver, schemaRegistry, errorReporter, files, modelAccessor);
            return statementCollector.CollectStatements();
        }

        private static bool MatchStatement(string projectDirectory, string relativeFilePath, bool embedStatements, IErrorReporter errorReporter)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            ICollection<SqlHint> hints = SqlHintParser.FromFile(inputFilePath, errorReporter).ToArray();
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
          , IReferencedAssemblyProvider referencedAssemblyProvider
          , IFileSystemProvider fileSystemProvider
          , ITypeResolverFacade typeResolver
          , IErrorReporter errorReporter
        )
        {
            IControllerActionTargetSelector controllerActionTargetSelector = new ControllerActionTargetSelector(productName, areaName, defaultOutputName, statements, referencedAssemblyProvider, errorReporter);
            return new ControllerDefinitionProvider(fileSystemProvider, controllerActionTargetSelector, typeResolver, errorReporter, endpoints).Controllers;
        }
    }
}