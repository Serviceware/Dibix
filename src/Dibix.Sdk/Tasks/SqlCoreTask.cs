using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk
{
    [Task("core")]
    [TaskProperty("ProjectName", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("ProjectDirectory", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("ProjectName", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("ConfigurationFilePath", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("LockFile", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("ResetLockFile", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("StaticCodeAnalysisSucceededFile", TaskPropertyType.String, Category = SqlCodeAnalysisCategory)]
    [TaskProperty("ResultsFile", TaskPropertyType.String, Category = SqlCodeAnalysisCategory)]
    [TaskProperty("ProductName", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("AreaName", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("Title", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("Version", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("Description", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("OutputDirectory", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("DefaultOutputName", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("ClientOutputName", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("ExternalAssemblyReferenceDir", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("Source", TaskPropertyType.Items, Category = GlobalCategory)]
    [TaskProperty("ScriptSource", TaskPropertyType.Items, Category = SqlCodeAnalysisCategory)]
    [TaskProperty("Contracts", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("Endpoints", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("References", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("DefaultSecuritySchemes", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("IsEmbedded", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("LimitDdlStatements", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("PreventDmlReferences", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("EnableExperimentalFeatures", TaskPropertyType.Boolean, Category = ArtifactGenerationCategory)]
    [TaskProperty("DatabaseSchemaProviderName", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("ModelCollation", TaskPropertyType.String, Category = GlobalCategory)]
    [TaskProperty("SqlReferencePath", TaskPropertyType.Items, Category = GlobalCategory)]
    [TaskProperty("NamingConventionPrefix", TaskPropertyType.String, Category = SqlCodeAnalysisCategory, Source = TaskPropertySource.UserDefined)]
    [TaskProperty("BaseUrl", TaskPropertyType.String, Category = ArtifactGenerationCategory, DefaultValue = "http://localhost", Source = TaskPropertySource.UserDefined)]
    public sealed partial class SqlCoreTask
    {
        private const string GlobalCategory = "SqlCore";
        private const string SqlCodeAnalysisCategory = "SqlCodeAnalysis";
        private const string ArtifactGenerationCategory = "ArtifactGeneration";

        public ICollection<string> AdditionalReferences { get; } = new Collection<string>();

        private partial bool Execute()
        {
            IActionParameterConverterRegistry actionParameterConverterRegistry = new ActionParameterConverterRegistry();
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();
            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(_configuration.SqlCore.ProjectDirectory);
            _configuration.AppendUserConfiguration(_configuration.SqlCore.ConfigurationFilePath, actionParameterSourceRegistry, actionParameterConverterRegistry, fileSystemProvider, _logger);

            if (_logger.HasLoggedErrors)
                return false;

            TSqlModel sqlModel = PublicSqlDataSchemaModelLoader.Load(_configuration.SqlCore, _logger);
            using (LockEntryManager lockEntryManager = LockEntryManager.Create(_configuration.SqlCore.ResetLockFile, _configuration.SqlCore.LockFile))
            {
                bool analysisResult = SqlCodeAnalysisTask.Execute(_configuration.SqlCore, _configuration.SqlCodeAnalysis, lockEntryManager, _logger, sqlModel);

                if (!analysisResult)
                    return false;

                bool codeGenerationResult = CodeGenerationTask.Execute
                (
                    _configuration.SqlCore
                  , _configuration.ArtifactGeneration
                  , actionParameterSourceRegistry
                  , actionParameterConverterRegistry
                  , lockEntryManager
                  , fileSystemProvider
                  , _logger
                  , sqlModel
                  , AdditionalReferences
                );

                return codeGenerationResult;
            }
        }
    }
}