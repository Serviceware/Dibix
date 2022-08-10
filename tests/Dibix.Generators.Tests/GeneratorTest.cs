using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                                                                  .AddReference<GeneratorTest>()
                                                                  .AddReference<TestMethodAttribute>()
                                                                  .AddReference<TestMethodGenerationAttribute>()
                                                                  .AddReference<SqlCodeAnalysisRule>()
                                                                  .AddReferences(MetadataReference.CreateFromFile(netStandardAssembly.Location))
                                                                  .AddReferences(MetadataReference.CreateFromFile(systemRuntimeAssembly.Location))
                                                                  .AddSyntaxTrees(syntaxTree);

            RoslynUtility.VerifyCompilation(inputCompilation);

            IIncrementalGenerator generator = new TestMethodGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            RoslynUtility.VerifyCompilation(runResult.Diagnostics);
            Assert.AreEqual(1, runResult.GeneratedTrees.Length);
            Assert.AreEqual(1, runResult.Results.Length);
            Assert.AreEqual(1, runResult.Results[0].GeneratedSources.Length);
            RoslynUtility.VerifyCompilation(runResult.Results[0]);
            RoslynUtility.VerifyCompilation(outputCompilation);
            RoslynUtility.VerifyCompilation(diagnostics);
            Assert.AreEqual(2, outputCompilation.SyntaxTrees.Count());
            Assert.AreEqual(inputCompilation.SyntaxTrees.First(), outputCompilation.SyntaxTrees.First());

            string expectedCode = this.GetExpectedText();
            string actualCode = outputCompilation.SyntaxTrees.ElementAt(1).ToString();
            base.AssertEqual(expectedCode, actualCode, "cs");
        }

        private string GetExpectedText([CallerMemberName] string? resourceName = null) => base.GetEmbeddedResourceContent($"{resourceName}.cs");
    }
}