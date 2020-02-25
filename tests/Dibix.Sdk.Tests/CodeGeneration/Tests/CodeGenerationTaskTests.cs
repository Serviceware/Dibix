using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dibix.Sdk.MSBuild;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed class CodeGenerationTaskTests : CodeGenerationTestBase
    {
        [Fact]
        public void NoMatchingSources_EmptyStatement()
        {
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql", embedStatements: false);
        }

        [Fact]
        public void External_Empty()
        {
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty.sql", embedStatements: false);
        }

        [Fact]
        public void Inline_SinglePrimitiveResult()
        {
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult.sql");
        }

        [Fact]
        public void Inline_SinglePrimitiveResult_WithoutDeclaration_Error()
        {
            ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(2,1) : error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
        }

        [Fact]
        public void Inline_SingleConcreteResult()
        {
            ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql"
              , @"Contracts\GenericContract.json"
            );
        }

        [Fact]
        public void Inline_MultiConcreteResult()
        {
            ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
              , @"Contracts\GenericContract.json"
            );
        }

        [Fact]
        public void Inline_SingleMultiMapResult()
        {
            ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_singlemultimapresult.sql"
              , @"Contracts\GenericContract.json"
              , @"Contracts\MultiMapContract.json"
            );
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,12) : error : Could not resolve contract 'X'");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,12) : error : Could not locate assembly: A");
        }

        [Fact]
        public void InvalidContractSchema_Error()
        {
            ExecuteTestAndExpectError(Enumerable.Empty<string>(), Enumerable.Repeat(@"Contracts\Invalid.json", 1), @"One or more errors occured during code generation:
Contracts\Invalid.json(3,12) : error : [JSON] Value ""x"" is not defined in enum. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] String 'x' does not match regex pattern '^#[A-Za-z]([\w]+)?\??(\[\]|\*?)?$'. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(2,14) : error : [JSON] Invalid type. Expected Array but got Object. (Invalid)
Contracts\Invalid.json(2,14) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid)");
        }

        private static void ExecuteTest(string source, bool embedStatements = true) => ExecuteTest(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), embedStatements);
        private static void ExecuteTest(string source, params string[] contracts) => ExecuteTest(Enumerable.Repeat(source, 1), contracts, true);
        private static void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, bool embedStatements)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"dibix-sdk-tests-{Guid.NewGuid()}");
            string defaultOutputFilePath = Path.Combine(tempDirectory, "TestAccessor.cs");
            Directory.CreateDirectory(tempDirectory);

            ICollection<CompilerError> errors = new Collection<CompilerError>();

            Mock<ITask> taskInstance = new Mock<ITask>(MockBehavior.Strict);
            Mock<IBuildEngine> buildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);

            taskInstance.SetupGet(x => x.BuildEngine).Returns(buildEngine.Object);
            buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                       .Callback((BuildErrorEventArgs e) => errors.Add(new CompilerError(e.File.Substring(DatabaseProjectDirectory.Length + 1), e.LineNumber, e.ColumnNumber, e.Code, e.Message)));
            
            bool result = CodeGenerationTask.Execute
            (
                projectDirectory: DatabaseProjectDirectory
              , productName: "Dibix.Sdk"
              , areaName: "Tests"
              , defaultOutputFilePath: defaultOutputFilePath
              , clientOutputFilePath: null
              , sources: sources
              , contracts: contracts
              , endpoints: null
              , references: null
              , embedStatements: embedStatements
              , logger: new TaskLoggingHelper(buildEngine.Object, nameof(CodeGenerationTask))
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            if (errors.Any())
                throw new CodeGenerationException(errors);

            Assert.True(result, "MSBuild task result was false");
            Assert.Empty(additionalAssemblyReferences);
            EvaluateFile(defaultOutputFilePath);
        }

        private static void ExecuteTestAndExpectError(string source, string expectedException) => ExecuteTestAndExpectError(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), expectedException);
        private static void ExecuteTestAndExpectError(IEnumerable<string> sources, IEnumerable<string> contracts, string expectedException)
        {
            try
            {
                ExecuteTest(sources, contracts, true);
                Assert.True(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.Equal(expectedException, ex.Message);
            }
        }
    }
}