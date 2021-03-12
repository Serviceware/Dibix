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
                isEmbedded: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql"
              , @"Tests\Syntax\dbx_tests_syntax_empty_nocompile.sql"
            );
        }

        [Fact]
        public void External_Empty()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_empty.sql", isEmbedded: false);
        }

        [Fact]
        public void External_Empty_WithParams()
        {
            this.ExecuteTest
            (
                isEmbedded: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
              , @"Types\dbx_codeanalysis_udt_generic.sql");
        }

        [Fact]
        public void External_Empty_WithParamsAndInputClass()
        {
            this.ExecuteTest
            (
                isEmbedded: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_params_inputclass.sql"
              , @"Types\dbx_codeanalysis_udt_generic.sql");
        }

        [Fact]
        public void External_Empty_WithOutputParam()
        {
            this.ExecuteTest
            (
                isEmbedded: false
              , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
            );
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
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(5,2,5,2):error:Missing return declaration for output. Please decorate the statement with the following hint to describe the output: -- @Return <ContractName>");
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

        [Fact(Skip = "Projection using the 'ResultType' property is currently only supported in a part of a grid result")]
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
        public void Inline_GridResultAsync()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_async.sql"
              , contracts: new [] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact]
        public void Inline_GridResult_AndSingleResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_single.sql"
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
        public void Inline_GridResult_WithCustomResultContractName_AndSingleResult()
        {
            this.ExecuteTest
            (
                source: @"Tests\Syntax\dbx_tests_syntax_gridresult_customname_single.sql"
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
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,21,1,21):error:Could not resolve type 'X'");
        }

        [Fact]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql", @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,23,1,23):error:Could not locate assembly: A
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,21,1,21):error:Could not resolve type 'X,A'");
        }

        [Fact]
        public void Inline_FileResult()
        {
            this.ExecuteTest(@"Tests\Syntax\dbx_tests_syntax_fileresult.sql");
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
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new []
                {
                    @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new [] { @"Endpoints\GenericEndpoint.json" }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "System.ComponentModel.DataAnnotations.dll"
                  , "Newtonsoft.Json.dll"
                }
            );
        }

        [Fact(Skip = "Output parameters are now supported")]
        public void Endpoint_WithOutputParam_Error()
        {
            this.ExecuteTestAndExpectError(@"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql", @"Endpoints\GenericEndpointWithOutputParam.json", @"One or more errors occured during code generation:
Endpoints\GenericEndpointWithOutputParam.json(6,18,6,18):error:Output parameters are not supported in endpoints: EmptyWithOutputParam");
        }

        [Fact]
        public void InvalidContractSchema_Error()
        {
            this.ExecuteTestAndExpectError(Enumerable.Repeat(@"Contracts\Invalid.json", 1), @"One or more errors occured during code generation:
Contracts\Invalid.json(3,12,3,12):error:String 'x' does not match regex pattern '^(binary|boolean|byte|datetime|datetimeoffset|decimal|double|float|int16|int32|int64|string|uuid|xml)\??\*?$'. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:String 'x' does not match regex pattern '^#([\w]+)(.([\w]+))*\??\*?$'. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:Invalid type. Expected Object but got String. (Invalid.A)
Contracts\Invalid.json(3,12,3,12):error:JSON does not match any schemas from 'anyOf'. (Invalid.A)
Contracts\Invalid.json(2,14,2,14):error:Invalid type. Expected Array but got Object. (Invalid)
Contracts\Invalid.json(2,14,2,14):error:JSON does not match any schemas from 'anyOf'. (Invalid)");
        }
    }
}