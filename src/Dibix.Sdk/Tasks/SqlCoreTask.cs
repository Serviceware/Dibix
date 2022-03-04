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
          , string outputName
          , string title
          , string version
          , string description
          , string defaultOutputFilePath
          , string endpointOutputFilePath
          , string clientOutputFilePath
          , string externalAssemblyReferenceDirectory
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , IEnumerable<TaskItem> contracts
          , IEnumerable<TaskItem> endpoints
          , IEnumerable<TaskItem> references
          , IEnumerable<TaskItem> defaultSecuritySchemes
          , bool isEmbedded
          , bool enableExperimentalFeatures
          , string databaseSchemaProviderName
          , string modelCollation
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
          , out string[] additionalAssemblyReferences
        )
        {
            IActionParameterConverterRegistry actionParameterConverterRegistry = new ActionParameterConverterRegistry();
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(projectDirectory);
            SqlCoreTaskConfiguration configuration = SqlCoreTaskConfiguration.Create(configurationFilePath, actionParameterSourceRegistry, actionParameterConverterRegistry, fileSystemProvider, logger);

            if (logger.HasLoggedErrors)
            {
                additionalAssemblyReferences = null;
                return false;
            }

            TSqlModel sqlModel = PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            using (LockEntryManager lockEntryManager = LockEntryManager.Create())
            {
                bool analysisResult = SqlCodeAnalysisTask.Execute
                (
                    projectName
                  , configuration.SqlCodeAnalysis
                  , isEmbedded
                  , staticCodeAnalysisSucceededFile
                  , resultsFile
                  , source
                  , scriptSource
                  , lockEntryManager
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
                  , outputName
                  , title
                  , version
                  , description
                  , configuration.Endpoints
                  , defaultOutputFilePath
                  , endpointOutputFilePath
                  , clientOutputFilePath
                  , externalAssemblyReferenceDirectory
                  , source
                  , contracts
                  , endpoints
                  , references
                  , defaultSecuritySchemes
                  , isEmbedded
                  , enableExperimentalFeatures
                  , databaseSchemaProviderName
                  , modelCollation
                  , sqlReferencePath
                  , actionParameterSourceRegistry
                  , actionParameterConverterRegistry
                  , lockEntryManager
                  , fileSystemProvider
                  , logger
                  , sqlModel
                  , out additionalAssemblyReferences
                );

                return codeGenerationResult;
            }
        }
    }
}