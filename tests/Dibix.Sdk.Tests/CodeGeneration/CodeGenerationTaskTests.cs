using System.Linq;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests
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
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(3,1) : Error : There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
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
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,12) : Error : Could not resolve type 'X'");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql", @"One or more errors occured during code generation:
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,12) : Error : Could not locate assembly: A
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,12) : Error : Could not resolve type 'X,A'");
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
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : String 'x' does not match regex pattern '^(binary|boolean|byte|datetime|datetimeoffset|decimal|double|float|int16|int32|int64|string|uuid)\??\*?$'. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : String 'x' does not match regex pattern '^#([\w]+)(.([\w]+))*\??\*?$'. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : JSON does not match any schemas from 'anyOf'. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : Invalid type. Expected Object but got String. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : Invalid type. Expected Object but got String. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : Invalid type. Expected Object but got String. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(3,12) : Error : JSON does not match any schemas from 'anyOf'. (Invalid.A)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(2,14) : Error : Invalid type. Expected Array but got Object. (Invalid)
G:\~Code\Dibix\tests\Dibix.Sdk.Tests.Database\Contracts\Invalid.json(2,14) : Error : JSON does not match any schemas from 'anyOf'. (Invalid)");
        }
    }
}