using System.Collections.Generic;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk
{
    public static class SqlCoreTask
    {
        public static bool Execute
        (
            string projectName
          , string projectDirectory
          , string configurationFilePath
          , string staticCodeAnalysisSucceededFile
          , string resultsFile
          , string productName
          , string areaName
          , string title
          , string version
          , string description
          , string defaultOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
          , out string[] additionalAssemblyReferences
        )
        {
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            SqlCoreTaskConfiguration configuration = SqlCoreTaskConfiguration.Create(configurationFilePath, actionParameterSourceRegistry, fileSystemProvider, logger);

            if (logger.HasLoggedErrors)
            {
                additionalAssemblyReferences = null;
                return false;
            }

            TSqlModel sqlModel = PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            bool analysisResult = SqlCodeAnalysisTask.Execute
            (
                projectName
              , configuration.SqlCodeAnalysis
              , isEmbedded
              , staticCodeAnalysisSucceededFile
              , resultsFile
              , source
              , scriptSource
              , logger
              , sqlModel
            );

            if (!analysisResult)
            {
                additionalAssemblyReferences = null;
                return false;
            }

            bool codeGenerationResult = CodeGenerationTask.Execute
            (
                projectName
              , projectDirectory
              , productName
              , areaName
              , title
              , version
              , description
              , configuration.Endpoints
              , defaultOutputFilePath
              , clientOutputFilePath
              , externalAssemblyReferenceDirectory
              , source
              , contracts
              , endpoints
              , references
              , defaultSecuritySchemes
              , isEmbedded
              , databaseSchemaProviderName
              , modelCollation
              , sqlReferencePath
              , actionParameterSourceRegistry
              , fileSystemProvider
              , logger
              , sqlModel
              , out additionalAssemblyReferences
            );

            return codeGenerationResult;
        }
    }
}