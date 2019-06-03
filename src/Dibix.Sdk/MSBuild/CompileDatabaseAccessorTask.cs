using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CompileDatabaseAccessorTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string @namespace
          , string targetDirectory
          , ICollection<string> artifacts
          , ICollection<string> contracts
          , ICollection<string> probingDirectories
          , bool isDML
          , TaskLoggingHelper logger
          , out string outputFilePath
        )
        {
            outputFilePath = Path.Combine(projectDirectory, targetDirectory, "SqlQueryAccessor.cs");

            ProbingAssemblyLocator assemblyLocator = new ProbingAssemblyLocator(probingDirectories ?? new string[0]);
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ICodeGenerationContext context = new StaticCodeGenerationContext(projectDirectory, @namespace, artifacts ?? new string[0], contracts ?? new string[0], assemblyLocator, isDML, errorReporter);
            ICodeGenerator generator = new DaoCodeGenerator(context);

            string generated = generator.Generate();
            if (!logger.HasLoggedErrors)
            {
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }
    }
}