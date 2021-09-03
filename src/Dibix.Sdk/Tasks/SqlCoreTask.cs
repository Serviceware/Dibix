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
          , string namingConventionPrefix
          , string staticCodeAnalysisSucceededFile
          , string resultsFile
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
            TSqlModel sqlModel = PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            bool analysisResult = SqlCodeAnalysisTask.Execute
            (
                projectName
              , namingConventionPrefix
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
              , baseUrl
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
              , logger
              , sqlModel
              , out additionalAssemblyReferences
            );

            return codeGenerationResult;
        }
    }
}