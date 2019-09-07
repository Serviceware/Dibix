using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class CompileDataAccessArtifactsTask
    { 
        public static bool Execute
        (
            string projectDirectory
          , string @namespace
          , string outputFilePath
          , ICollection<string> sources
          , ICollection<string> contracts
          , ICollection<string> endpoints
          , ICollection<string> references
          , bool multipleAreas
          , bool isDML
          , TaskLoggingHelper logger
          , out string[] detectedReferences
        )
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            StaticCodeGenerationContext context = new StaticCodeGenerationContext(projectDirectory, @namespace, sources ?? new string[0], contracts ?? new string[0], endpoints ?? new string[0], references ?? new string[0], multipleAreas, isDML, errorReporter);
            ICodeGenerator generator = new DaoCodeGenerator(context);

            string generated = generator.Generate();
            detectedReferences = context.Configuration.Output.DetectedReferences.ToArray();
            if (!logger.HasLoggedErrors)
            {
                File.WriteAllText(outputFilePath, generated);
                return true;
            }

            return false;
        }
    }
}