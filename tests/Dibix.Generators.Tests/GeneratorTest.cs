using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Generators.Tests
{
    [TestClass]
    public sealed class GeneratorTest : TestBase
    {
        [TestMethod]
        public void TestMethodGenerator()
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText($"[assembly: {typeof(TestMethodGenerationAttribute)}(typeof(Dibix.Sdk.CodeAnalysis.SqlCodeAnalysisRule), \"Dibix.Generators.Tests\")]");
            Assembly netStandardAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "netstandard");
            Assembly systemRuntimeAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "System.Runtime");
            CSharpCompilation inputCompilation = CSharpCompilation.Create(null)
                                                                  .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                  .AddReference<Type>()
                                                                  .AddReference<TestMethodAttribute>()
                                                                  .AddReference<SqlCodeAnalysisRule>()
                                                                  .AddReferences(MetadataReference.CreateFromFile(netStandardAssembly.Location))
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
                "TestMethodGenerationAttribute.generated.cs",
                "SqlCodeAnalysisRuleTests.generated.cs"
            };
            for (int i = 1; i < syntaxTrees.Count; i++)
            {
                SyntaxTree outputSyntaxTree = syntaxTrees[i];
                FileInfo outputFile = new FileInfo(outputSyntaxTree.FilePath);
                string actualCode = outputSyntaxTree.ToString();
                this.AddResultFile(outputFile.Name, actualCode);
                Assert.AreEqual(expectedFiles[i - 1], outputFile.Name);
                string expectedCode = this.GetEmbeddedResourceContent(outputFile.Name);
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