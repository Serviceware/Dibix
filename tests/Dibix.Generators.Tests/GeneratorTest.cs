using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Testing;
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
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText("[assembly: Dibix.Generators.TestMethodGeneration(typeof(Dibix.Sdk.CodeAnalysis.SqlCodeAnalysisRule), \"Dibix.Generators.Tests\")]");
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
                string expectedCode = this.GetEmbeddedResourceContent(outputFile.Name).Replace("%GENERATORVERSION%", TestUtility.GeneratorFileVersion);
                this.AssertEqual(expectedCode, actualCode, outputName: Path.GetFileNameWithoutExtension(outputFile.Name), extension: outputFile.Extension.TrimStart('.'));
            }

            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);

            Assert.AreEqual(expectedFiles.Length, runResult!.GeneratedTrees.Length);
            Assert.AreEqual(expectedFiles.Length, runResult!.Results[0].GeneratedSources.Length);
            Assert.AreEqual(expectedFiles.Length + 1, syntaxTrees.Count);
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
    [TaskProperty(""EnableExperimentalFeatures"", TaskPropertyType.Boolean, Category = ""Endpoint"")]
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
                string expectedCode = this.GetEmbeddedResourceContent(outputFile.Name).Replace("%GENERATORVERSION%", TestUtility.GeneratorFileVersion);
                this.AssertEqual(expectedCode, actualCode, outputName: Path.GetFileNameWithoutExtension(outputFile.Name), extension: outputFile.Extension.TrimStart('.'));
            }

            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);

            Assert.AreEqual(expectedFiles.Length, runResult!.GeneratedTrees.Length);
            Assert.AreEqual(expectedFiles.Length, runResult!.Results[0].GeneratedSources.Length);
            Assert.AreEqual(expectedFiles.Length + 1, syntaxTrees.Count);
        }
    }
}