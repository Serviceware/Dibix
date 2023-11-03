using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    [TaskProperty("ExternalAssemblyReferenceDirectory", TaskPropertyType.String, Category = ArtifactGenerationCategory)]
    [TaskProperty("Source", TaskPropertyType.Items, Category = GlobalCategory)]
    [TaskProperty("ScriptSource", TaskPropertyType.Items, Category = SqlCodeAnalysisCategory)]
    [TaskProperty("Contracts", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("Endpoints", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("References", TaskPropertyType.Items, Category = ArtifactGenerationCategory)]
    [TaskProperty("IsEmbedded", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("LimitDdlStatements", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("PreventDmlReferences", TaskPropertyType.Boolean, Category = GlobalCategory)]
    [TaskProperty("SupportOpenApiNullableReferenceTypes", TaskPropertyType.Boolean, Category = ArtifactGenerationCategory)]
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
            CodeAnalysis.SqlCodeAnalysisConfiguration sqlCodeAnalysisConfiguration = new CodeAnalysis.SqlCodeAnalysisConfiguration
            {
                IsEmbedded = _configuration.SqlCore.IsEmbedded,
                LimitDdlStatements = _configuration.SqlCore.LimitDdlStatements,
                StaticCodeAnalysisSucceededFile = _configuration.SqlCodeAnalysis.StaticCodeAnalysisSucceededFile,
                ResultsFile = _configuration.SqlCodeAnalysis.ResultsFile,
                NamingConventionPrefix = _configuration.SqlCodeAnalysis.NamingConventionPrefix
            };
            sqlCodeAnalysisConfiguration.Source.AddRange(_configuration.SqlCore.Source);
            sqlCodeAnalysisConfiguration.ScriptSource.AddRange(_configuration.SqlCodeAnalysis.ScriptSource);

            CodeGenerationConfiguration codeGenerationConfiguration = new CodeGenerationConfiguration
            {
                ProductName = _configuration.ArtifactGeneration.ProductName,
                AreaName = _configuration.ArtifactGeneration.AreaName,
                IsEmbedded = _configuration.SqlCore.IsEmbedded,
                LimitDdlStatements = _configuration.SqlCore.LimitDdlStatements,
                ProjectDirectory = _configuration.SqlCore.ProjectDirectory,
                OutputDirectory = _configuration.ArtifactGeneration.OutputDirectory,
                ExternalAssemblyReferenceDirectory = _configuration.ArtifactGeneration.ExternalAssemblyReferenceDirectory,
                DefaultOutputName = _configuration.ArtifactGeneration.DefaultOutputName,
                ClientOutputName = _configuration.ArtifactGeneration.ClientOutputName,
                Title = _configuration.ArtifactGeneration.Title,
                Version = _configuration.ArtifactGeneration.Version,
                Description = _configuration.ArtifactGeneration.Description,
                SupportOpenApiNullableReferenceTypes = _configuration.ArtifactGeneration.SupportOpenApiNullableReferenceTypes
            };
            codeGenerationConfiguration.Source.AddRange(_configuration.SqlCore.Source);
            codeGenerationConfiguration.Contracts.AddRange(_configuration.ArtifactGeneration.Contracts);
            codeGenerationConfiguration.Endpoints.AddRange(_configuration.ArtifactGeneration.Endpoints);
            codeGenerationConfiguration.References.AddRange(_configuration.ArtifactGeneration.References);

            IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(_configuration.SqlCore.ProjectDirectory);
            SecuritySchemes securitySchemes = new SecuritySchemes();
            IActionParameterConverterRegistry actionParameterConverterRegistry = new ActionParameterConverterRegistry();
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();

            if (File.Exists(_configuration.SqlCore.ConfigurationFilePath))
            {
                UserConfigurationLoader userConfigurationLoader = new UserConfigurationLoader
                (
                    _configuration.SqlCore.ConfigurationFilePath
                  , fileSystemProvider
                  , _logger
                  , new SqlCodeAnalysisUserConfigurationReader(sqlCodeAnalysisConfiguration)
                  , new CodeGenerationUserConfigurationReader(codeGenerationConfiguration, securitySchemes, actionParameterSourceRegistry, actionParameterConverterRegistry, _logger)
                );
                userConfigurationLoader.Load();
            }

            if (_logger.HasLoggedErrors)
                return false;

            TSqlModel sqlModel = PublicSqlDataSchemaModelLoader.Load
            (
                preventDmlReferences: _configuration.SqlCore.PreventDmlReferences
              , databaseSchemaProviderName: _configuration.SqlCore.DatabaseSchemaProviderName
              , modelCollation: _configuration.SqlCore.ModelCollation
              , source: _configuration.SqlCore.Source
              , sqlReferencePath: _configuration.SqlCore.SqlReferencePath
              , logger: _logger
            );

            using (LockEntryManager lockEntryManager = LockEntryManager.Create(_configuration.SqlCore.ResetLockFile, _configuration.SqlCore.LockFile))
            {
                bool analysisResult = SqlCodeAnalysisTask.Execute(sqlCodeAnalysisConfiguration, lockEntryManager, _logger, sqlModel);

                if (!analysisResult)
                    return false;

                bool codeGenerationResult = CodeGenerationTask.Execute
                (
                    codeGenerationConfiguration
                  , securitySchemes
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