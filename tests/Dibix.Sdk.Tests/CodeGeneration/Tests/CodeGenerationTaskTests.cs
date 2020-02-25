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
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql");
        }

        [Fact]
        public void External_Empty()
        {
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty.sql");
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
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(2,1) : error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithBuiltinResultContract()
        {
            ExecuteTest
            (
                contract: @"Contracts\GenericContract.json"
              , source: @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_builtinresultcontract.sql"
            );
        }

        [Fact]
        public void Inline_MultiConcreteResult_WithBuiltinResultContract()
        {
            ExecuteTest
            (
                contract: @"Contracts\GenericContract.json"
              , source: @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult_builtinresultcontract.sql"
            );
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql", @"One or more errors occured during code generation:
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,12) : error : Could not resolve contract 'X'");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql", @"One or more errors occured during code generation:
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,12) : error : Could not locate assembly: A");
        }

        [Fact]
        public void InvalidContractSchema_Error()
        {
            ExecuteTestAndExpectError(Enumerable.Empty<string>(), Enumerable.Repeat(@"Contracts\Invalid.json", 1), @"One or more errors occured during code generation:
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] Value ""x"" is not defined in enum. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] String 'x' does not match regex pattern '^#[A-Za-z]([\w]+)?\??(\[\]|\*?)?$'. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(2,14) : error : [JSON] Invalid type. Expected Array but got Object. (Invalid)
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(2,14) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid)");
        }

        private static void ExecuteTest(string source) => ExecuteTest(Enumerable.Repeat(source, 1), Enumerable.Empty<string>());
        private static void ExecuteTest(string source, string contract) => ExecuteTest(Enumerable.Repeat(source, 1), Enumerable.Repeat(contract, 1));
        private static void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"dibix-sdk-tests-{Guid.NewGuid()}");
            string defaultOutputFilePath = Path.Combine(tempDirectory, "TestAccessor.cs");
            Directory.CreateDirectory(tempDirectory);

            ICollection<CompilerError> errors = new Collection<CompilerError>();

            Mock<ITask> taskInstance = new Mock<ITask>(MockBehavior.Strict);
            Mock<IBuildEngine> buildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);

            taskInstance.SetupGet(x => x.BuildEngine).Returns(buildEngine.Object);
            buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                       .Callback((BuildErrorEventArgs e) => errors.Add(new CompilerError(e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message)));
            
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
              , embedStatements: false
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
                ExecuteTest(sources, contracts);
                Assert.True(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.Equal(expectedException, ex.Message);
            }
        }
    }
}