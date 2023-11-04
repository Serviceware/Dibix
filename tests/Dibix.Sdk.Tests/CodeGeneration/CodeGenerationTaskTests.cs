using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    [TestClass]
    public sealed partial class CodeGenerationTaskTests
    {
        [TestMethod]
        public void NoMatchingSources_EmptyStatement()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_nocompile.sql"
                }
            );
        }

        [TestMethod]
        public void External_Empty()
        {
            ExecuteTest(sources: new[] { @"Tests\Syntax\dbx_tests_syntax_empty.sql" }, isEmbedded: false);
        }

        [TestMethod]
        public void External_Empty_WithParams()
        {
            ExecuteTest
            (
                isEmbedded: false
              , contracts: new[] { @"Contracts\Direction.json" }
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                }
            );
        }

        [TestMethod]
        public void External_Empty_WithParamsAndInputClass()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_params_inputclass.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                }
            );
        }

        [TestMethod]
        public void External_Empty_WithOutputParam()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[] { @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql" }
            );
        }

        [TestMethod]
        public void Inline_SinglePrimitiveResult()
        {
            ExecuteTest(sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult.sql" });
        }

        [TestMethod]
        public void Inline_SinglePrimitiveResult_Async()
        {
            ExecuteTest(sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_async.sql" });
        }

        [TestMethod]
        public void Inline_SinglePrimitiveResult_WithoutDeclaration_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql" }
              , expectedException: @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql(5,2,5,2):error:Missing return declaration for output. Please decorate the statement with the following hint to describe the output: -- @Return <ContractName>"
            );
        }

        [TestMethod]
        public void Inline_SingleOrDefaultPrimitiveResult_WithModeSingleOrDefault_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleordefaultprimitiveresult_nonnullable.sql" }
              , expectedException: @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleordefaultprimitiveresult_nonnullable.sql(1,21,1,21):error:When using the result mode option 'SingleOrDefault', the primitive return type must be nullable: int32"
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql" }
              , contracts: new[] { @"Contracts\GenericContract.json" }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_MultiConcreteResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql" }
              , contracts: new[] { @"Contracts\GenericContract.json" }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_SingleMultiMapResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singlemultimapresult.sql" }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        [Ignore("Projection using the 'ResultType' property is currently only supported in a part of a grid result")]
        public void Inline_SingleMultiMapResult_WithProjection()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singlemultimapresult_projection.sql" }
              , contracts: new[]
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult.sql" }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResultAsync()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_async.sql" }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_AndSingleResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_single.sql" }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_WithCustomResultContractName()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_customname.sql" }
              , contracts: new[] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_WithCustomResultContractName_AndSingleResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_customname_single.sql" }
              , contracts: new[] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_WithExistingResultContract()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_existingresultcontract.sql" }
              , contracts: new[] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                  , @"Contracts\Grid\GridResult.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_MergeResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_merge.sql" }
              , contracts: new[] 
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_GridResult_WithProjection()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_gridresult_projection.sql" }
              , contracts: new[] 
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql" }
              , expectedException: @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql(1,21,1,21):error:Could not resolve type 'X'"
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql" }
              , expectedException: @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql(1,21,1,21):error:Could not resolve type 'X,A'");
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithInvalidMarkup_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invalidmarkup.sql" }
              , expectedException: @"One or more errors occured during code generation:
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invalidmarkup.sql(4,4,4,4):error:Unexpected markup element 'Wtf'
Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invalidmarkup.sql(3,38,3,38):error:Unexpected @Return property 'Wtf'");
        }

        [TestMethod]
        public void Inline_FileResult()
        {
            ExecuteTest(sources: new[] { @"Tests\Syntax\dbx_tests_syntax_fileresult.sql" });
        }

        [TestMethod]
        public void Enum1()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[] { @"Tests\Syntax\dbx_tests_syntax_enum1.sql" }
            );
        }

        [TestMethod]
        public void Enum2()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[] { @"Tests\Syntax\dbx_tests_syntax_enum2.sql" }
            );
        }

        [TestMethod]
        public void Enum3()
        {
            ExecuteTest
            (
                isEmbedded: false
              , sources: new[] { @"Tests\Syntax\dbx_tests_syntax_enum3.sql" }
            );
        }

        [TestMethod]
        public void Client()
        {
            ExecuteTest
            (
                sources: new[]
                {
                    @"Tests\dbx_tests_authorization.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params_array.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\AnotherInputContract.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Client
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Dibix.Http.Client.dll"
                  , "Dibix.Http.Server.dll"
                  , "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                  , "System.Net.Http.dll"
                }
            );
        }

        [TestMethod]
        public void Endpoints()
        {
            ExecuteTest
            (
                sources: new[]
                {
                    @"Tests\dbx_tests_authorization.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params_array.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\AnotherInputContract.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Endpoint
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Dibix.Http.Server.dll"
                  , "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Endpoints_Accessor()
        {
            ExecuteTest
            (
                sources: new[]
                {
                    @"Tests\dbx_tests_authorization.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params_array.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\AnotherInputContract.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Accessor
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Dibix.Http.Server.dll"
                  , "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Endpoints_Accessor_Model()
        {
            ExecuteTest
            (
                sources: new[]
                {
                    @"Tests\dbx_tests_authorization.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params_array.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\AnotherInputContract.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Model
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Dibix.Http.Server.dll"
                  , "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Endpoints_OpenApi()
        {
            ExecuteTest
            (
                sources: new[]
                {
                    @"Tests\dbx_tests_authorization.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileupload.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_params_array.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\AnotherInputContract.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.OpenApi
              , expectedAdditionalAssemblyReferences: new[]
                {
                    "Dibix.Http.Server.dll"
                  , "Newtonsoft.Json.dll"
                  , "System.ComponentModel.DataAnnotations.dll"
                }
            );
        }

        [TestMethod]
        public void Endpoint_WithValidationErrors_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[]
                {
                    @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\Request.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpointWithErrors.json" }
              , isEmbedded: false
              , expectedException: @"One or more errors occured during code generation:
Endpoints\GenericEndpointWithErrors.json(8,15,8,15):error:Unknown property source 'WTF'
Endpoints\GenericEndpointWithErrors.json(19,20,19,20):error:Property 'X' not found on contract 'Dibix.Sdk.Tests.DomainModel.Request'
Endpoints\GenericEndpointWithErrors.json(20,23,20,23):error:Property 'Nm' not found on contract 'Dibix.Sdk.Tests.DomainModel.Entry'
Endpoints\GenericEndpointWithErrors.json(25,27,25,27):error:Property 'Nm' not found on contract 'Dibix.Sdk.Tests.DomainModel.Entry'
Endpoints\GenericEndpointWithErrors.json(9,19,9,19):error:Source 'ENV' does not support property 'MachinePassword'
Endpoints\GenericEndpointWithErrors.json(17,27,17,27):error:The path segment 'get' is a known HTTP verb, which should be indicated by the action method and is therefore redundant: this/get/is/wrong
Endpoints\GenericEndpointWithErrors.json(34,22,34,22):error:Equivalent paths are not allowed: GET Tests/GenericEndpoint/ambiguous/child/route/{a}
Endpoints\GenericEndpointWithErrors.json(40,22,40,22):error:Equivalent paths are not allowed: GET Tests/GenericEndpoint/ambiguous/child/route/{b}"
            );
        }

        [TestMethod]
        [Ignore("Output parameters are now supported")]
        public void Endpoint_WithOutputParam_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql" }
              , endpoints: new[] { @"Endpoints\GenericEndpointWithOutputParam.json" }
              , expectedException: @"One or more errors occured during code generation:
Endpoints\GenericEndpointWithOutputParam.json(6,18,6,18):error:Output parameters are not supported in endpoints: EmptyWithOutputParam"
            );
        }

        [TestMethod]
        public void DuplicateContract_Error()
        {
            ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicateContract.json"
                  , @"Contracts\AccessRights.json"
                }
              , expectedException: @"One or more errors occured during code generation:
Contracts\DuplicateContract.json(5,12,5,12):error:Property with the name 'Invalid' already exists in the current JSON object. Path 'Invalid', line 5, position 12."
            );
        }

        [TestMethod]
        public void DuplicatePropertyName_Error()
        {
            ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicatePropertyName.json"
                  , @"Contracts\AccessRights.json"
                }
              , expectedException: @"One or more errors occured during code generation:
Contracts\DuplicatePropertyName.json(4,9,4,9):error:Property with the name 'AA' already exists in the current JSON object. Path 'Invalid.AA', line 4, position 9."
            );
        }

        [TestMethod]
        public void DuplicatePropertyNameCaseInsensitive_Error()
        {
            ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicatePropertyNameCaseInsensitive.json"
                  , @"Contracts\AccessRights.json"
                }
              , expectedException: @"One or more errors occured during code generation:
Contracts\DuplicatePropertyNameCaseInsensitive.json(3,6,3,6):error:Property with the name 'AAA' already exists in the current JSON object. Path 'Invalid.AAA', line 3, position 6.
Contracts\DuplicatePropertyNameCaseInsensitive.json(4,6,4,6):error:Property with the name 'AAa' already exists in the current JSON object. Path 'Invalid.AAa', line 4, position 6.
Contracts\DuplicatePropertyNameCaseInsensitive.json(5,6,5,6):error:Property with the name 'Aaa' already exists in the current JSON object. Path 'Invalid.Aaa', line 5, position 6."
            );
        }
    }
}
