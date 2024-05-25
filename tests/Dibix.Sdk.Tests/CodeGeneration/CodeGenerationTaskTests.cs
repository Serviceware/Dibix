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
                  , @"Types\dbx_codeanalysis_udt_int.sql"
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
            );
        }

        [TestMethod]
        public void Inline_SingleOrDefaultPrimitiveResult_WithModeSingleOrDefault_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleordefaultprimitiveresult_nonnullable.sql" }
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql" }
              , contracts: new[] { @"Contracts\GenericContract.json" }
            );
        }

        [TestMethod]
        public void Inline_MultiConcreteResult()
        {
            ExecuteTest
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql" }
              , contracts: new[] { @"Contracts\GenericContract.json" }
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
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql" }
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql" }
            );
        }

        [TestMethod]
        public void Inline_SingleConcreteResult_WithInvalidMarkup_Error()
        {
            ExecuteTestAndExpectError
            (
                sources: new[] { @"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invalidmarkup.sql" }
            );
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
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
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
                  , @"Contracts\AnotherInputContractData.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Client
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
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
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
                  , @"Contracts\AnotherInputContractData.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Endpoint
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
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
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
                  , @"Contracts\AnotherInputContractData.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Accessor
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
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
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
                  , @"Contracts\AnotherInputContractData.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.Model
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
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
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
                  , @"Contracts\AnotherInputContractData.json"
                  , @"Contracts\AnotherEntry.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpoint.json" }
              , isEmbedded: false
              , outputKind: AssertOutputKind.OpenApi
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
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_missingcolumn.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\Request.json"
                }
              , endpoints: new[] { @"Endpoints\GenericEndpointWithErrors.json" }
              , isEmbedded: false
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
            );
        }
    }
}
