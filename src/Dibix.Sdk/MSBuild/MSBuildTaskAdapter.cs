using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class MSBuildTaskAdapter
    {
        public static bool Initialize(string projectDirectory, string @namespace, IEnumerable<string> assemblyReferences, IEnumerable<string> inputs, TaskLoggingHelper logger)
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);

            foreach (string codeGenerationTarget in inputs ?? Enumerable.Empty<string>())
            {
                string configurationPath = Path.GetFullPath(Path.Combine(projectDirectory, codeGenerationTarget));
                string configurationDirectory = Path.GetDirectoryName(configurationPath);
                string configurationName = Path.GetFileNameWithoutExtension(configurationPath);
                string outputPath = $"{Path.Combine(configurationDirectory, configurationName)}.cs";

                IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(configurationDirectory);

                string json = File.ReadAllText(configurationPath);
                SimpleJsonGeneratorConfigurationReader reader = new SimpleJsonGeneratorConfigurationReader(json, fileSystemProvider, errorReporter);
                GeneratorConfiguration configuration = new GeneratorConfiguration();
                reader.Read(configuration);

                IAssemblyLocator assemblyLocator = new AssemblyLocator(projectDirectory, assemblyReferences ?? Enumerable.Empty<string>());
                ICodeGenerationContext context = new ProjectFileCodeGenerationContext(configuration, @namespace, configurationName, assemblyLocator, errorReporter);
                ICodeGenerator generator = new CodeGenerator(context);

                string expected = File.ReadAllText(outputPath);
                string actual = generator.Generate();

                if (!errorReporter.HasErrors // Generated output will change in case of error
                 && expected != actual)
                    logger.LogError(null, null, null, codeGenerationTarget, 0, 0, 0, 0, "Generated code is not up to date");
            }

            return !logger.HasLoggedErrors;
        }
    }
}
