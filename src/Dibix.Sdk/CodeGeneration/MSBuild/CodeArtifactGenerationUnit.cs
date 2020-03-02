﻿using System.IO;
using Dibix.Sdk.MSBuild;

namespace Dibix.Sdk.CodeGeneration.MSBuild
{
    internal abstract class CodeArtifactGenerationUnit
    {
        public abstract bool ShouldGenerate(CodeArtifactsGenerationModel model);
        public abstract bool Generate(CodeArtifactsGenerationModel model, IErrorReporter errorReporter);
    }

    internal abstract class CodeArtifactGenerationUnit<TGenerator> : CodeArtifactGenerationUnit where TGenerator : CodeGenerator
    {
        public override bool Generate(CodeArtifactsGenerationModel model, IErrorReporter errorReporter)
        {
            TGenerator generator = this.CreateGenerator(errorReporter);

            string generated = generator.Generate(model);

            if (!errorReporter.HasErrors)
            {
                string outputFilePath = this.GetOutputFilePath(model);
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }

        protected abstract TGenerator CreateGenerator(IErrorReporter errorReporter);

        protected abstract string GetOutputFilePath(CodeArtifactsGenerationModel model);
    }
}