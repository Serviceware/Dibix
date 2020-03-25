﻿using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("codegen")]
    internal sealed class CodeGenerationTaskRunner : TaskRunner
    {
        public CodeGenerationTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            CodeGenerationTask.Execute
            (
                projectDirectory: configuration.GetSingleValue<string>("ProjectDirectory")
              , productName: configuration.GetSingleValue<string>("ProductName")
              , areaName: configuration.GetSingleValue<string>("AreaName")
              , defaultOutputFilePath: configuration.GetSingleValue<string>("DefaultOutputFilePath")
              , clientOutputFilePath: configuration.GetSingleValue<string>("ClientOutputFilePath")
              , source: configuration.GetItems("Source")
              , contracts: configuration.GetItems("Contracts")
              , endpoints: configuration.GetItems("Endpoints")
              , references: configuration.GetItems("References")
              , embedStatements: configuration.GetSingleValue<bool>("EmbedStatements")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
              , additionalAssemblyReferences: out string[] _
            );
        }
    }
}