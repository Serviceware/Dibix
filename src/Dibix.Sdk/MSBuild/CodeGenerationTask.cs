using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CodeGenerationTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> sources
          , ICollection<string> contracts
          , ICollection<string> endpoints
          , ICollection<string> references
          , bool embedStatements
          , TaskLoggingHelper logger
          , out string[] detectedReferences
        )
        {
            return ExecuteCore
            (
                projectDirectory
              , productName
              , areaName
              , defaultOutputFilePath
              , clientOutputFilePath
              , sources ?? new string[0]
              , contracts ?? new string[0]
              , endpoints ?? new string[0]
              , references ?? new string[0]
              , embedStatements
              , logger
              , out detectedReferences
            );
        }

        private static bool ExecuteCore
        (
            string projectDirectory
          , string productName
          , string areaName
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , ICollection<string> sources
          , IEnumerable<string> contracts
          , IEnumerable<string> endpoints
          , ICollection<string> references
          , bool embedStatements
          , TaskLoggingHelper logger
          , out string[] detectedReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);

            string rootNamespace = NamespaceUtility.BuildRootNamespace(productName, areaName);
            string defaultOutputName = Path.GetFileNameWithoutExtension(defaultOutputFilePath).Replace(".", String.Empty);

            GeneratorConfiguration configuration = new GeneratorConfiguration();
            configuration.Output.GeneratePublicArtifacts = true;
            configuration.Output.WriteNamespaces = true;
            configuration.Output.ProductName = productName;
            configuration.Output.AreaName = areaName;

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            ISqlStatementParser parser = new SqlStoredProcedureParser();
            ISqlStatementFormatter formatter = embedStatements ? (ISqlStatementFormatter)new TakeSourceSqlStatementFormatter() : new ExecStoredProcedureSqlStatementFormatter();
            ContractDefinitionProvider contractDefinitionProvider = new ContractDefinitionProvider(fileSystemProvider, errorReporter, contracts, productName, areaName);
            IContractResolverFacade contractResolver = new ContractResolverFacade(new DefaultAssemblyLocator(projectDirectory, references));
            contractResolver.RegisterContractResolver(new ContractDefinitionResolver(contractDefinitionProvider), 0);
            IDatabaseAccessorStatementProvider databaseAccessorStatementProvider = new DatabaseAccessorStatementProvider(parser, formatter, contractResolver, errorReporter, projectDirectory, embedStatements, productName, areaName);
            ICollection<SqlStatementInfo> statements = databaseAccessorStatementProvider.CollectStatements(sources).ToArray();
            IControllerActionTargetSelector controllerActionTargetSelector = new ControllerActionTargetSelector(productName, areaName, defaultOutputName, projectDirectory, statements, references, errorReporter);
            ControllerDefinitionProvider controllerDefinitionProvider = new ControllerDefinitionProvider(fileSystemProvider, controllerActionTargetSelector, errorReporter, endpoints);
            UserDefinedTypeProvider userDefinedTypeProvider = new UserDefinedTypeProvider(sources, errorReporter, productName, areaName);

            CodeArtifactsGenerationContext context = new CodeArtifactsGenerationContext
            (
                projectDirectory
              , productName
              , areaName
              , rootNamespace
              , defaultOutputFilePath
              , defaultOutputName
              , clientOutputFilePath
              , configuration
              , statements
              , sources
              , controllerDefinitionProvider.Controllers
              , userDefinedTypeProvider.Types
              , references
              , embedStatements
              , fileSystemProvider
              , contractDefinitionProvider
              , contractResolver
              , errorReporter
            );

            ICodeArtifactsGenerator generator = new CodeArtifactsGenerator();
            bool result = generator.Generate(context);
            detectedReferences = context.DetectedReferences.ToArray();
            return result;
        }
    }
}