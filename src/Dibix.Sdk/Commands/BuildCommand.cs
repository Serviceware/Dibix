using System.IO;
using System.Threading.Tasks;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;

namespace Dibix.Sdk
{
    [CommandLineAction("build", "Performs code analysis and generates artifacts from a database project.")]
    [CommandLineInputProperty("ProjectName", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("ProjectDirectory", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("ProjectPath", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("ProjectName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("ConfigurationFilePath", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("LockFile", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("ResetLockFile", CommandLineInputPropertyType.Boolean, Category = GlobalCategory)]
    [CommandLineInputProperty("StaticCodeAnalysisSucceededFile", CommandLineInputPropertyType.String, Category = SqlCodeAnalysisCategory)]
    [CommandLineInputProperty("ResultsFile", CommandLineInputPropertyType.String, Category = SqlCodeAnalysisCategory)]
    [CommandLineInputProperty("ProductName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("AreaName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("Title", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("OpenApiVersion", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("OpenApiDescription", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("OpenApiSchemaVersion", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("OutputDirectory", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("AccessorTargetName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("AccessorTargetFileName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("EndpointTargetFileName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("PackageMetadataTargetFileName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("ClientTargetFileName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("ModelTargetFileName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("DocumentationTargetName", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("ExternalAssemblyReferenceDirectory", CommandLineInputPropertyType.String, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("Source", CommandLineInputPropertyType.Items, Category = GlobalCategory)]
    [CommandLineInputProperty("ScriptSource", CommandLineInputPropertyType.Items, Category = SqlCodeAnalysisCategory)]
    [CommandLineInputProperty("Contracts", CommandLineInputPropertyType.Items, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("Endpoints", CommandLineInputPropertyType.Items, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("References", CommandLineInputPropertyType.Items, Category = ArtifactGenerationCategory)]
    [CommandLineInputProperty("IsEmbedded", CommandLineInputPropertyType.Boolean, Category = GlobalCategory)]
    [CommandLineInputProperty("LimitDdlStatements", CommandLineInputPropertyType.Boolean, Category = GlobalCategory)]
    [CommandLineInputProperty("PreventDmlReferences", CommandLineInputPropertyType.Boolean, Category = GlobalCategory)]
    [CommandLineInputProperty("DatabaseSchemaProviderName", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("ModelCollation", CommandLineInputPropertyType.String, Category = GlobalCategory)]
    [CommandLineInputProperty("SqlReferencePath", CommandLineInputPropertyType.Items, Category = GlobalCategory)]
    public sealed partial class BuildCommand
    {
        private const string GlobalCategory = "Build";
        private const string SqlCodeAnalysisCategory = "SqlCodeAnalysis";
        private const string ArtifactGenerationCategory = "ArtifactGeneration";

        public async partial Task<int> Execute(BuildCommandInput input)
        {
            SqlCodeAnalysisConfiguration sqlCodeAnalysisConfiguration = new SqlCodeAnalysisConfiguration
            {
                IsEmbedded = input.Build.IsEmbedded,
                LimitDdlStatements = input.Build.LimitDdlStatements,
                StaticCodeAnalysisSucceededFile = input.SqlCodeAnalysis.StaticCodeAnalysisSucceededFile,
                ResultsFile = input.SqlCodeAnalysis.ResultsFile
            };
            sqlCodeAnalysisConfiguration.Source.AddRange(input.Build.Source);
            sqlCodeAnalysisConfiguration.ScriptSource.AddRange(input.SqlCodeAnalysis.ScriptSource);

            CodeGenerationConfiguration codeGenerationConfiguration = new CodeGenerationConfiguration
            {
                ProductName = input.ArtifactGeneration.ProductName,
                AreaName = input.ArtifactGeneration.AreaName,
                IsEmbedded = input.Build.IsEmbedded,
                LimitDdlStatements = input.Build.LimitDdlStatements,
                ProjectDirectory = input.Build.ProjectDirectory,
                ProjectPath = input.Build.ProjectPath,
                OutputDirectory = input.ArtifactGeneration.OutputDirectory,
                ExternalAssemblyReferenceDirectory = input.ArtifactGeneration.ExternalAssemblyReferenceDirectory,
                AccessorTargetName = input.ArtifactGeneration.AccessorTargetName,
                AccessorTargetFileName = input.ArtifactGeneration.AccessorTargetFileName,
                EndpointTargetFileName = input.ArtifactGeneration.EndpointTargetFileName,
                PackageMetadataTargetFileName = input.ArtifactGeneration.PackageMetadataTargetFileName,
                ClientTargetFileName = input.ArtifactGeneration.ClientTargetFileName,
                ModelTargetFileName = input.ArtifactGeneration.ModelTargetFileName,
                DocumentationTargetName = input.ArtifactGeneration.DocumentationTargetName,
                Title = input.ArtifactGeneration.Title,
                OpenApiVersion = input.ArtifactGeneration.OpenApiVersion,
                OpenApiDescription = input.ArtifactGeneration.OpenApiDescription,
                OpenApiSchemaVersion = input.ArtifactGeneration.OpenApiSchemaVersion,
            };
            codeGenerationConfiguration.Source.AddRange(input.Build.Source);
            codeGenerationConfiguration.Contracts.AddRange(input.ArtifactGeneration.Contracts);
            codeGenerationConfiguration.Endpoints.AddRange(input.ArtifactGeneration.Endpoints);
            codeGenerationConfiguration.References.AddRange(input.ArtifactGeneration.References);

            SecuritySchemes securitySchemes = new SecuritySchemes();
            IActionParameterConverterRegistry actionParameterConverterRegistry = new ActionParameterConverterRegistry();
            IActionParameterSourceRegistry actionParameterSourceRegistry = new ActionParameterSourceRegistry();

            if (File.Exists(input.Build.ConfigurationFilePath))
            {
                UserConfigurationLoader userConfigurationLoader = new UserConfigurationLoader
                (
                    input.Build.ConfigurationFilePath
                  , _logger
                  , new SqlCodeAnalysisUserConfigurationReader(sqlCodeAnalysisConfiguration)
                  , new CodeGenerationUserConfigurationReader(codeGenerationConfiguration, securitySchemes, actionParameterSourceRegistry, actionParameterConverterRegistry, _logger)
                );
                userConfigurationLoader.Load();
            }

            if (_logger.HasLoggedErrors)
                return -1;

            using PublicSqlDataSchemaModel publicSqlDataSchemaModel = PublicSqlDataSchemaModelLoader.Load
            (
                preventDmlReferences: input.Build.PreventDmlReferences
              , databaseSchemaProviderName: input.Build.DatabaseSchemaProviderName
              , modelCollation: input.Build.ModelCollation
              , source: input.Build.Source
              , sqlReferencePath: input.Build.SqlReferencePath
              , logger: _logger
            );

            using LockEntryManager lockEntryManager = LockEntryManager.Create(input.Build.ResetLockFile, input.Build.LockFile);
            bool analysisResult = SqlCodeAnalysisTask.Execute(sqlCodeAnalysisConfiguration, lockEntryManager, _logger, publicSqlDataSchemaModel.Model);

            if (!analysisResult)
                return -1;

            bool codeGenerationResult = await CodeGenerationTask.Execute
            (
                codeGenerationConfiguration
              , securitySchemes
              , actionParameterSourceRegistry
              , actionParameterConverterRegistry
              , lockEntryManager
              , _logger
              , publicSqlDataSchemaModel.Model
            ).ConfigureAwait(false);

            int exitCode = codeGenerationResult ? 0 : -1;
            return exitCode;
        }
    }
}