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
            this.ExecuteTest
            (
                embedStatements: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql"
              , @"Tests\Syntax\dbx_tests_syntax_empty_nocompile.sql"
            );
        }

        [Fact]
        public void External_Empty()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty.sql", embedStatements: false);
        }

        [Fact]
        public void External_Empty_WithParams()
        {
            this.ExecuteTest
            (
                embedStatements: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
              , @"Types\dbx_codeanalysis_udt_generic.sql");
        }

        [Fact]
        public void External_Empty_WithParamsAndInputClass()
        {
            this.ExecuteTest
            (
                embedStatements: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_params_inputclass.sql"
              , @"Types\dbx_codeanalysis_udt_generic.sql");
        }

        [Fact]
        public void Inline_SinglePrimitiveResult()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult.sql");
        }

        [Fact]
        public void Inline_SinglePrimitiveResult_Async()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_async.sql");
        }

        [Fact]
        public void Inline_SinglePrimitiveResult_WithoutDeclaration_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(3,1) : error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
        }

        [Fact]
        public void Inline_SingleConcreteResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql"
              , contract: @"Contracts\GenericContract.json"
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_MultiConcreteResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
              , contract: @"Contracts\GenericContract.json"
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_SingleMultiMapResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_singlemultimapresult.sql"
              , contracts: new [] 
                {
                    @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        //[Fact]
        public void Inline_SingleMultiMapResult_WithProjection()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_singlemultimapresult_projection.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult_WithCustomResultContractName()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_customname.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult_WithExistingResultContract()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_existingresultcontract.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                  , @"Contracts\Grid\GridResult.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult_MergeResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_merge.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult_WithProjection()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_projection.sql"
              , contracts: new [] 
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,12) : error : Could not resolve contract 'X'");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,12) : error : Could not locate assembly: A");
        }

        [Fact]
        public void Inline_FileApi()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_fileapi.sql");
        }

        [Fact]
        public void DomainModel()
        {
            this.ExecuteTest
            (
                new []
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            );
        }

        [Fact]
        public void Endpoints()
        {
            this.ExecuteTest
            (
                sources: new [] 
                { 
                    @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                }
              , contracts: new [] { @"Contracts\GenericContract.json" }
              , endpoints: new [] { @"Endpoints\GenericEndpoint.json" }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void InvalidContractSchema_Error()
        {
            this.ExecuteTestAndExpectError(Enumerable.Empty<string>(), Enumerable.Repeat(@"Contracts\Invalid.json", 1), @"One or more errors occured during code generation:
Contracts\Invalid.json(3,12) : error : [JSON] Value ""x"" is not defined in enum. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] String 'x' does not match regex pattern '^#([\w]+)(.([\w]+))*\??\*?$'. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(2,14) : error : [JSON] Invalid type. Expected Array but got Object. (Invalid)
Contracts\Invalid.json(2,14) : error : [JSON] JSON does not match any schemas from 'anyOf'. (Invalid)");
        }

        private void ExecuteTest(string source, bool embedStatements = true) => this.ExecuteTest(embedStatements, source);
        private void ExecuteTest(bool embedStatements = true, params string[] sources) => this.ExecuteTest(sources, Enumerable.Empty<string>(), Enumerable.Empty<string>(), embedStatements, false, false, Enumerable.Empty<string>());
        private void ExecuteTest(string source, string contract, params string[] expectedAdditionalAssemblyReferences) => this.ExecuteTest(source, Enumerable.Repeat(contract, 1), expectedAdditionalAssemblyReferences);
        private void ExecuteTest(string source, IEnumerable<string> contracts, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(Enumerable.Repeat(source, 1), contracts, true, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> contracts) => this.ExecuteTest(Enumerable.Empty<string>(), contracts, Enumerable.Empty<string>(), true, true, false, Enumerable.Empty<string>());
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, bool embedStatements, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, Enumerable.Empty<string>(), embedStatements, false, false, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, IEnumerable<string> expectedAdditionalAssemblyReferences) => this.ExecuteTest(sources, contracts, endpoints, true, false, true, expectedAdditionalAssemblyReferences);
        private void ExecuteTest(IEnumerable<string> sources, IEnumerable<string> contracts, IEnumerable<string> endpoints, bool embedStatements, bool generateClient, bool assertOpenApi, IEnumerable<string> expectedAdditionalAssemblyReferences)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"dibix-sdk-tests-{Guid.NewGuid()}");
            string outputFilePath = Path.Combine(tempDirectory, "TestAccessor.cs");
            Directory.CreateDirectory(tempDirectory);

            ICollection<CompilerError> errors = new Collection<CompilerError>();

            Mock<ITask> task = new Mock<ITask>(MockBehavior.Strict);
            Mock<IBuildEngine> buildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);

            task.SetupGet(x => x.BuildEngine).Returns(buildEngine.Object);
            buildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()));
            buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                       .Callback((BuildErrorEventArgs e) => errors.Add(new CompilerError(e.File.Substring(DatabaseProjectDirectory.Length + 1), e.LineNumber, e.ColumnNumber, e.Code, e.Message)));
            
            bool result = CodeGenerationTask.Execute
            (
                projectDirectory: DatabaseProjectDirectory
              , productName: "Dibix.Sdk"
              , areaName: "Tests"
              , defaultOutputFilePath: !generateClient ? outputFilePath : null
              , clientOutputFilePath: generateClient ? outputFilePath : null
              , source: sources.Select(x =>
                {
                    Mock<ITaskItem> item = new Mock<ITaskItem>(MockBehavior.Strict);
                    item.SetupGet(y => y.MetadataNames).Returns(new string[0]);
                    item.Setup(y => y.GetMetadata("FullPath")).Returns(Path.Combine(DatabaseProjectDirectory, x));
                    return item.Object;
                }).ToArray()
              , contracts: contracts
              , endpoints: endpoints
              , references: null
              , embedStatements: embedStatements
              , databaseSchemaProviderName: this.DatabaseSchemaProviderName
              , modelCollation: this.ModelCollation
              , sqlReferencePath: new ITaskItem[0]
              , task: task.Object
              , logger: new TaskLoggingHelper(buildEngine.Object, nameof(CodeGenerationTask))
              , additionalAssemblyReferences: out string[] additionalAssemblyReferences
            );

            if (errors.Any())
                throw new CodeGenerationException(errors);

            Assert.True(result, "MSBuild task result was false");
            Assert.Equal(expectedAdditionalAssemblyReferences, additionalAssemblyReferences);
            EvaluateFile(outputFilePath);

            if (assertOpenApi)
            {
                string openApiDocumentFilePath = Path.Combine(tempDirectory, "Tests.yml");
                EvaluateFile($"{TestName}_OpenApi", openApiDocumentFilePath);
            }
        }

        private void ExecuteTestAndExpectError(string source, string expectedException) => this.ExecuteTestAndExpectError(Enumerable.Repeat(source, 1), Enumerable.Empty<string>(), expectedException);
        private void ExecuteTestAndExpectError(IEnumerable<string> sources, IEnumerable<string> contracts, string expectedException)
        {
            try
            {
                this.ExecuteTest(sources, contracts, true, Enumerable.Empty<string>());
                Assert.True(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.Equal(expectedException, ex.Message);
            }
        }
    }
}