﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CompileDatabaseAccessorTask
    { 
        public static bool Execute(string projectDirectory, string targetDirectory, IEnumerable<string> inputs, ICollection<string> probingDirectories, TaskLoggingHelper logger, out string outputFilePath, out ICollection<string> referencePaths)
        {
            outputFilePath = Path.Combine(projectDirectory, targetDirectory, "SqlQueryAccessor.cs");

            ProbingAssemblyLocator assemblyLocator = new ProbingAssemblyLocator(probingDirectories ?? new string[0]);
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ICodeGenerationContext context = new GlobalCodeGenerationContext(projectDirectory, inputs ?? Enumerable.Empty<string>(), assemblyLocator, errorReporter);
            ICodeGenerator generator = new CodeGenerator(context);

            string generated = generator.Generate();
            if (!logger.HasLoggedErrors)
            {
                File.WriteAllText(outputFilePath, generated);
                referencePaths = assemblyLocator.ReferencePaths;
                return true;
            }

            referencePaths = new string[0];
            return false;
        }
    }
}