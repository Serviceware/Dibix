using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Generators;
using Dibix.Testing;
using Dibix.Testing.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dibix.Generators.Tests
{
    [TestClass]
    public sealed class GeneratorTest : TestBase
    {
        [TestMethod]
        public void TestMethodGenerator()
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText("[assembly: Dibix.Testing.Generators.TestMethodGeneration(typeof(Dibix.Sdk.CodeAnalysis.SqlCodeAnalysisRule), \"Dibix.Generators.Tests\")]");
            Assembly systemRuntimeAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "System.Runtime");
            CSharpCompilation inputCompilation = CSharpCompilation.Create(null)
                                                                  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                  .AddReference<Type>()
                                                                  .AddReference<TestMethodAttribute>()
                                                                  .AddReference<SqlCodeAnalysisRule>()
                                                                  .AddReferences(MetadataReference.CreateFromFile(systemRuntimeAssembly.Location))
                                                                  .AddSyntaxTrees(syntaxTree);

            // At this state the compilation isn't valid because the generator will add the post initialization outputs that are used within this compilation
            //RoslynUtility.VerifyCompilation(inputCompilation);

            IIncrementalGenerator generator = new TestMethodGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            RoslynUtility.VerifyCompilation(runResult.Diagnostics);
            Assert.AreEqual(1, runResult.Results.Length);
            RoslynUtility.VerifyCompilation(runResult.Results[0]);
            IList<SyntaxTree> syntaxTrees = outputCompilation.SyntaxTrees.ToArray();
            Assert.AreEqual(inputCompilation.SyntaxTrees[0], syntaxTrees[0]);

            string[] expectedFiles =
            {
                "TestMethodGenerationAttribute.g.cs",
                "SqlCodeAnalysisRuleTests.g.cs"
            };
            for (int i = 1; i < syntaxTrees.Count; i++)
            {
                SyntaxTree outputSyntaxTree = syntaxTrees[i];
                FileInfo outputFile = new FileInfo(outputSyntaxTree.FilePath);
                string actualCode = outputSyntaxTree.ToString();
                this.AddResultFile(outputFile.Name, actualCode);
                Assert.AreEqual(expectedFiles[i - 1], outputFile.Name);
                string expectedCode = this.GetEmbeddedResourceContent(outputFile.Name).Replace("%GENERATORVERSION%", GetGeneratorVersion(typeof(TestMethodGenerator)));
                this.AssertEqual(expectedCode, actualCode, outputName: Path.GetFileNameWithoutExtension(outputFile.Name), extension: outputFile.Extension.TrimStart('.'));
            }

            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);

            Assert.AreEqual(expectedFiles.Length, runResult!.GeneratedTrees.Length);
            Assert.AreEqual(expectedFiles.Length, runResult!.Results[0].GeneratedSources.Length);
            Assert.AreEqual(expectedFiles.Length + 1, syntaxTrees.Count);
        }

        [TestMethod]
        public void EmbeddedResourceAccessorGenerator()
        {
            CSharpCompilation inputCompilation = CSharpCompilation.Create(null)
                                                                  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                  .AddReference<Type>();

            RoslynUtility.VerifyCompilation(inputCompilation);

            Mock<AdditionalText> additionalText1 = new Mock<AdditionalText>(MockBehavior.Strict);
            Mock<AdditionalText> additionalText2 = new Mock<AdditionalText>(MockBehavior.Strict);
            Mock<AdditionalText> additionalText3 = new Mock<AdditionalText>(MockBehavior.Strict);
            Mock<AdditionalText> additionalText4 = new Mock<AdditionalText>(MockBehavior.Strict);
            additionalText1.SetupGet(x => x.Path).Returns(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "SomeText.yml"));
            additionalText2.SetupGet(x => x.Path).Returns("");
            additionalText3.SetupGet(x => x.Path).Returns("");
            additionalText4.SetupGet(x => x.Path).Returns(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, "AnotherText.json"));

            Mock<AnalyzerConfigOptions> globalAnalyzerConfigOptions = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptionsProvider> analyzerConfigOptionsProvider = new Mock<AnalyzerConfigOptionsProvider>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptions> fileAnalyzerConfigOptions1 = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptions> fileAnalyzerConfigOptions2 = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptions> fileAnalyzerConfigOptions3 = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptions> fileAnalyzerConfigOptions4 = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            analyzerConfigOptionsProvider.SetupGet(x => x.GlobalOptions).Returns(globalAnalyzerConfigOptions.Object);
            analyzerConfigOptionsProvider.Setup(x => x.GetOptions(additionalText1.Object)).Returns(fileAnalyzerConfigOptions1.Object);
            analyzerConfigOptionsProvider.Setup(x => x.GetOptions(additionalText2.Object)).Returns(fileAnalyzerConfigOptions2.Object);
            analyzerConfigOptionsProvider.Setup(x => x.GetOptions(additionalText3.Object)).Returns(fileAnalyzerConfigOptions3.Object);
            analyzerConfigOptionsProvider.Setup(x => x.GetOptions(additionalText4.Object)).Returns(fileAnalyzerConfigOptions4.Object);
            globalAnalyzerConfigOptions.Setup(x => x.TryGetValue("build_property.rootnamespace", out It.Ref<string?>.IsAny))
                                       .Returns((string _, out string? value) =>
                                       {
                                           value = typeof(GeneratorTest).Namespace;
                                           return true;
                                       });
            fileAnalyzerConfigOptions1.Setup(x => x.TryGetValue("build_metadata.embeddedresource.generateaccessor", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "True";
                                          return true;
                                      });
            fileAnalyzerConfigOptions1.Setup(x => x.TryGetValue("build_metadata.embeddedresource.logicalname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "SomeText";
                                          return true;
                                      });
            fileAnalyzerConfigOptions1.Setup(x => x.TryGetValue("build_metadata.embeddedresource.accessorname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = null;
                                          return true;
                                      });
            fileAnalyzerConfigOptions2.Setup(x => x.TryGetValue("build_metadata.embeddedresource.generateaccessor", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "False";
                                          return true;
                                      });
            fileAnalyzerConfigOptions2.Setup(x => x.TryGetValue("build_metadata.embeddedresource.logicalname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = null;
                                          return true;
                                      });
            fileAnalyzerConfigOptions2.Setup(x => x.TryGetValue("build_metadata.embeddedresource.accessorname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = null;
                                          return true;
                                      });
            fileAnalyzerConfigOptions3.Setup(x => x.TryGetValue("build_metadata.embeddedresource.generateaccessor", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = null;
                                          return false;
                                      });
            fileAnalyzerConfigOptions3.Setup(x => x.TryGetValue("build_metadata.embeddedresource.logicalname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = null;
                                          return true;
                                      });
            fileAnalyzerConfigOptions3.Setup(x => x.TryGetValue("build_metadata.embeddedresource.accessorname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "";
                                          return true;
                                      });
            fileAnalyzerConfigOptions4.Setup(x => x.TryGetValue("build_metadata.embeddedresource.generateaccessor", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "True";
                                          return true;
                                      });
            fileAnalyzerConfigOptions4.Setup(x => x.TryGetValue("build_metadata.embeddedresource.logicalname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "Dibix.Generators.Tests.Resources.AnotherText.json";
                                          return true;
                                      });
            fileAnalyzerConfigOptions4.Setup(x => x.TryGetValue("build_metadata.embeddedresource.accessorname", out It.Ref<string?>.IsAny))
                                      .Returns((string _, out string? value) =>
                                      {
                                          value = "AnotherResource";
                                          return true;
                                      });

            IIncrementalGenerator generator = new EmbeddedResourceAccessorGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create
            (
                generators: EnumerableExtensions.Create(generator.AsSourceGenerator())
              , additionalTexts: EnumerableExtensions.Create(additionalText1.Object, additionalText2.Object, additionalText3.Object, additionalText4.Object)
              , optionsProvider: analyzerConfigOptionsProvider.Object
            );

            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            RoslynUtility.VerifyCompilation(runResult.Diagnostics);
            Assert.AreEqual(1, runResult.Results.Length);
            RoslynUtility.VerifyCompilation(runResult.Results[0]);
            IList<SyntaxTree> syntaxTrees = outputCompilation.SyntaxTrees.ToArray();

            string[] expectedFiles =
            [
                "Resource.g.cs",
                "AnotherResource.g.cs"
            ];
            for (int i = 0; i < syntaxTrees.Count; i++)
            {
                SyntaxTree outputSyntaxTree = syntaxTrees[i];
                FileInfo outputFile = new FileInfo(outputSyntaxTree.FilePath);
                string actualCode = outputSyntaxTree.ToString();
                AddResultFile(outputFile.Name, actualCode);
                Assert.AreEqual(expectedFiles[i], outputFile.Name);
                string expectedCode = GetEmbeddedResourceContent(outputFile.Name).Replace("%GENERATORVERSION%", GetGeneratorVersion(typeof(EmbeddedResourceAccessorGenerator)));
                AssertEqual(expectedCode, actualCode, outputName: Path.GetFileNameWithoutExtension(outputFile.Name), extension: outputFile.Extension.TrimStart('.'));
            }

            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);

            Assert.AreEqual(expectedFiles.Length, runResult!.GeneratedTrees.Length);
            Assert.AreEqual(expectedFiles.Length, runResult!.Results[0].GeneratedSources.Length);
            Assert.AreEqual(expectedFiles.Length, syntaxTrees.Count);
        }

        [TestMethod]
        public void TaskGenerator()
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"using Dibix.Sdk.Abstractions;

namespace Dibix.Generators.Tests.Tasks
{
    [Task(""core"")]
    [TaskProperty(""ProjectName"", TaskPropertyType.String)]
    [TaskProperty(""AreaName"", TaskPropertyType.String, Source = TaskPropertySource.UserDefined)]
    [TaskProperty(""SqlReferencePath"", TaskPropertyType.Items)]
    [TaskProperty(""NamingConventionPrefix"", TaskPropertyType.String, DefaultValue = """")]
    [TaskProperty(""BaseUrl"", TaskPropertyType.String, Category = ""Endpoint"", DefaultValue = ""http://localhost"")]
    public sealed partial class SqlCoreTask
    {
        private partial bool Execute() => true;
    }
}");
            Assembly netStandardAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "netstandard");
            Assembly systemRuntimeAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "System.Runtime");
            CSharpCompilation inputCompilation = CSharpCompilation.Create(null)
                                                                  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                  .AddReference<Type>()
                                                                  .AddReference<TaskAttribute>()
                                                                  .AddReferences(MetadataReference.CreateFromFile(netStandardAssembly.Location))
                                                                  .AddReferences(MetadataReference.CreateFromFile(systemRuntimeAssembly.Location))
                                                                  .AddSyntaxTrees(syntaxTree);

            // At this state the compilation isn't valid because the generator will add the post initialization outputs that are used within this compilation
            //RoslynUtility.VerifyCompilation(inputCompilation);

            Mock<AnalyzerConfigOptionsProvider> analyzerConfigOptionsProvider = new Mock<AnalyzerConfigOptionsProvider>(MockBehavior.Strict);
            Mock<AnalyzerConfigOptions> globalAnalyzerConfigOptions = new Mock<AnalyzerConfigOptions>(MockBehavior.Strict);
            globalAnalyzerConfigOptions.Setup(x => x.TryGetValue("build_property.rootnamespace", out It.Ref<string?>.IsAny))
                                       .Returns((string _, out string? value) =>
                                       {
                                           value = typeof(GeneratorTest).Namespace;
                                           return true;
                                       });
            analyzerConfigOptionsProvider.SetupGet(x => x.GlobalOptions).Returns(globalAnalyzerConfigOptions.Object);

            IIncrementalGenerator generator = new TaskGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create
            (
                generators: EnumerableExtensions.Create(generator.AsSourceGenerator())
              , optionsProvider: analyzerConfigOptionsProvider.Object
            );

            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            RoslynUtility.VerifyCompilation(runResult.Diagnostics);
            Assert.AreEqual(1, runResult.Results.Length);
            RoslynUtility.VerifyCompilation(runResult.Results[0]);
            IList<SyntaxTree> syntaxTrees = outputCompilation.SyntaxTrees.ToArray();
            Assert.AreEqual(inputCompilation.SyntaxTrees[0], syntaxTrees[0]);

            string[] expectedFiles =
            {
                "SqlCoreTask.g.cs",
                "SqlCoreTaskConfiguration.g.cs",
                "EndpointConfiguration.g.cs"
            };
            for (int i = 1; i < syntaxTrees.Count; i++)
            {
                SyntaxTree outputSyntaxTree = syntaxTrees[i];
                FileInfo outputFile = new FileInfo(outputSyntaxTree.FilePath);
                string actualCode = outputSyntaxTree.ToString();
                this.AddResultFile(outputFile.Name, actualCode);
                Assert.AreEqual(expectedFiles[i - 1], outputFile.Name);
                string expectedCode = this.GetEmbeddedResourceContent(outputFile.Name).Replace("%GENERATORVERSION%", GetGeneratorVersion(typeof(TaskGenerator)));
                this.AssertEqual(expectedCode, actualCode, outputName: Path.GetFileNameWithoutExtension(outputFile.Name), extension: outputFile.Extension.TrimStart('.'));
            }

            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);

            Assert.AreEqual(expectedFiles.Length, runResult!.GeneratedTrees.Length);
            Assert.AreEqual(expectedFiles.Length, runResult!.Results[0].GeneratedSources.Length);
            Assert.AreEqual(expectedFiles.Length + 1, syntaxTrees.Count);
        }

        private static string? GetGeneratorVersion(Type generatorType)
        {
            // We could use ThisAssembly.AssemblyFileVersion here, but during development, the tests might already use a newer version than the generator compilation.
            // This happens, if a new git commit is created locally, but the generator has not been rebuilt incrementally, because it hasn't changed its source.
            return FileVersionInfo.GetVersionInfo(generatorType.Assembly.Location).FileVersion;
        }
    }
}