using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

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
          , IEnumerable<string> sources
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , IEnumerable<string> references
          , bool embedStatements
          , IErrorReporter errorReporter)
        {
            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string defaultOutputName = defaultOutputFilePath != null ? Path.GetFileNameWithoutExtension(defaultOutputFilePath).Replace(".", String.Empty) : "SqlQueryAccessor";

            ICollection<string> normalizedSources = NormalizePaths(sources, projectDirectory).ToArray();
            ICollection<string> normalizedContracts = NormalizePaths(contracts, projectDirectory).ToArray();
            ICollection<string> normalizedEndpoints = NormalizePaths(endpoints, projectDirectory).ToArray();
            ICollection<string> normalizedReferences = NormalizePaths(references, projectDirectory).ToArray();

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            ISqlStatementFormatter formatter = embedStatements ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            IContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, errorReporter, normalizedContracts, productName, areaName);
            IContractResolverFacade contractResolver = new ContractResolverFacade(new DefaultAssemblyLocator(projectDirectory, normalizedReferences));
            contractResolver.RegisterContractResolver(new ContractDefinitionResolver(contractDefinitionProvider), 0);

            CodeArtifactsGenerationModel model = new CodeArtifactsGenerationModel(CodeGeneratorCompatibilityLevel.Full)
            {
                AreaName = areaName,
                RootNamespace = rootNamespace,
                DefaultClassName = defaultOutputName,
                ProductName = productName,
                DefaultOutputFilePath = defaultOutputFilePath,
                ClientOutputFilePath = clientOutputFilePath
            };
            model.Statements.AddRange(CollectStatements(normalizedSources, projectDirectory, productName, areaName, embedStatements, formatter, contractResolver, errorReporter));
            model.UserDefinedTypes.AddRange(CollectUserDefinedTypes(normalizedSources, productName, areaName, errorReporter));
            model.Contracts.AddRange(contractDefinitionProvider.Contracts);
            model.Controllers.AddRange(CollectControllers(normalizedEndpoints, projectDirectory, productName, areaName, defaultOutputName, model.Statements, normalizedReferences, fileSystemProvider, errorReporter));

            return model;
        }

        private static IEnumerable<string> NormalizePaths(IEnumerable<string> paths, string projectDirectory)
        {
            return (paths ?? Enumerable.Empty<string>()).Select(x => Path.IsPathRooted(x) ? x : Path.Combine(projectDirectory, x));
        }

        private static IEnumerable<SqlStatementInfo> CollectStatements
        (
            IEnumerable<string> sources
          , string projectDirectory
          , string productName
          , string areaName
          , bool embedStatements
          , ISqlStatementFormatter formatter
          , IContractResolverFacade contractResolver
          , IErrorReporter errorReporter)
        {
            IEnumerable<string> files = sources.Where(x => MatchStatement(projectDirectory, x, embedStatements, errorReporter));
            ISqlStatementParser parser = new SqlStoredProcedureParser();
            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector(productName, areaName, parser, formatter, contractResolver, errorReporter, files);
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

        private static IEnumerable<UserDefinedTypeDefinition> CollectUserDefinedTypes(IEnumerable<string> sources, string productName, string areaName, IErrorReporter errorReporter)
        {
            return new UserDefinedTypeProvider(sources, errorReporter, productName, areaName).Types;
        }

        private static IEnumerable<ControllerDefinition> CollectControllers
        (
            IEnumerable<string> endpoints
          , string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputName
          , ICollection<SqlStatementInfo> statements
          , IEnumerable<string> references
          , IFileSystemProvider fileSystemProvider
          , IErrorReporter errorReporter)
        {
            IControllerActionTargetSelector controllerActionTargetSelector = new ControllerActionTargetSelector(productName, areaName, defaultOutputName, projectDirectory, statements, references, errorReporter);
            return new ControllerDefinitionProvider(fileSystemProvider, controllerActionTargetSelector, errorReporter, endpoints).Controllers;
        }
    }
}