using System;
using System.CodeDom.Compiler;
using System.Collections;
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
            ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_primitiveresult.sql");
        }

        [Fact]
        public void Inline_SinglePrimitiveResult_WithoutDeclaration_Error()
        {
            try
            {
                ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_primitiveresult_invaliddeclaration.sql");
                Assert.True(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.Equal(@"One or more errors occured during code generation:
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_primitiveresult_invaliddeclaration.sql(2,1) : error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>
D:\Serviceware\Common\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_primitiveresult_invaliddeclaration.sql(2,1) : error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>", ex.Message);
            }
        }

        private static void ExecuteTest(params string[] sources)
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
              , contracts: null
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
    }
}