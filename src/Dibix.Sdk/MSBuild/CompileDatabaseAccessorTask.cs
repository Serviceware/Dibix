using System.Collections.Generic;
using System.IO;
using System.Linq;
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
          , IEnumerable<string> inputs
          , ICollection<string> probingDirectories
          , bool isDML
          , TaskLoggingHelper logger
          , out string outputFilePath
          , out ICollection<string> referencePaths
        )
        {
            outputFilePath = Path.Combine(projectDirectory, targetDirectory, "SqlQueryAccessor.cs");

            ProbingAssemblyLocator assemblyLocator = new ProbingAssemblyLocator(probingDirectories ?? new string[0]);
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ICodeGenerationContext context = new GlobalCodeGenerationContext(projectDirectory, @namespace, inputs ?? Enumerable.Empty<string>(), assemblyLocator, isDML, errorReporter);
            ICodeGenerator generator = new DaoCodeGenerator(context);

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