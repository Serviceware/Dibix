using System.IO;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk
{
    public class VerifyGeneratedCodeTask : Task, ITask
    {
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string[] AssemblyReferences { get; set; }
        public string[] Inputs { get; set; }

        public override bool Execute()
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(base.Log);

            foreach (string codeGenerationTarget in this.Inputs)
            {
                string configurationPath = Path.GetFullPath(Path.Combine(this.ProjectDirectory, codeGenerationTarget));
                string configurationDirectory = Path.GetDirectoryName(configurationPath);
                string configurationName = Path.GetFileNameWithoutExtension(configurationPath);
                string outputPath = $"{Path.Combine(configurationDirectory, configurationName)}.cs";

                IFileSystemProvider fileSystemProvider = new PhysicalFileSystemProvider(configurationDirectory);

                string json = File.ReadAllText(configurationPath);
                SimpleJsonGeneratorConfigurationReader reader = new SimpleJsonGeneratorConfigurationReader(json, fileSystemProvider, errorReporter);
                GeneratorConfiguration configuration = new GeneratorConfiguration();
                reader.Read(configuration);

                IAssemblyLocator assemblyLocator = new AssemblyLocator(this.ProjectDirectory, this.AssemblyReferences);
                ICodeGenerationContext context = new ProjectFileCodeGenerationContext(configuration, this.Namespace, configurationName, assemblyLocator, errorReporter);
                ICodeGenerator generator = new CodeGenerator(context);

                string expected = File.ReadAllText(outputPath);
                string actual = generator.Generate();

                if (!errorReporter.HasErrors // Generated output will change in case of error
                 && expected != actual)
                    base.Log.LogError(null, null, null, codeGenerationTarget, 0, 0, 0, 0, "Generated code is not up to date");
            }

            return !base.Log.HasLoggedErrors;
        }
    }
}
