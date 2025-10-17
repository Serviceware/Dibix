using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    [TestClass]
    public sealed partial class CodeGenerationTaskTests
    {
        [TestMethod]
        public async Task NoMatchingSources_EmptyStatement()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_undeclared.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_nocompile.sql"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task External_Empty()
        {
            await ExecuteTest(sources: [@"Tests\Syntax\dbx_tests_syntax_empty.sql"], isEmbedded: false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task External_Empty_WithParams()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , contracts: [@"Contracts\Direction.json"]
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task External_Empty_WithParamsAndInputClass()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: new[]
                {
                    @"Tests\Syntax\dbx_tests_syntax_empty_params_inputclass.sql"
                  , @"Types\dbx_codeanalysis_udt_generic.sql"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task External_Empty_WithOutputParam()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: [@"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SinglePrimitiveResult()
        {
            await ExecuteTest(sources: [@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SinglePrimitiveResult_Async()
        {
            await ExecuteTest(sources: [@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_async.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SinglePrimitiveResult_WithoutDeclaration_Error()
        {
            await ExecuteTestAndExpectError(sources: [@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invaliddeclaration.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleOrDefaultPrimitiveResult_WithModeSingleOrDefault_Error()
        {
            await ExecuteTestAndExpectError(sources: [@"Tests\Syntax\dbx_tests_syntax_singleordefaultprimitiveresult_nonnullable.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleConcreteResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql"]
              , contracts: [@"Contracts\GenericContract.json"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_MultiConcreteResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_multiconcreteresult.sql"]
              , contracts: new[] { @"Contracts\GenericContract.json" }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleMultiMapResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_singlemultimapresult.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        [Ignore("Projection using the 'ResultType' property is currently only supported in a part of a grid result")]
        public async Task Inline_SingleMultiMapResult_WithProjection()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_singlemultimapresult_projection.sql"]
              , contracts: new[]
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResultAsync()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_async.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_AndSingleResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_single.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_WithCustomResultContractName()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_customname.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_WithCustomResultContractName_AndSingleResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_customname_single.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_WithExistingResultContract()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_existingresultcontract.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                  , @"Contracts\Grid\GridResult.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_MergeResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_merge.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\Extension\MultiMapContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_GridResult_WithProjection()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_gridresult_projection.sql"]
              , contracts: new[]
                {
                    @"Contracts\AccessRights.json"
                  , @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\JointContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleConcreteResult_WithUnknownResultContract_Error()
        {
            await ExecuteTestAndExpectError(sources: [@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontract.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleConcreteResult_WithUnknownResultContractAssembly_Error()
        {
            await ExecuteTestAndExpectError(sources: [@"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_unknownresultcontractassembly.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_SingleConcreteResult_WithInvalidMarkup_Error()
        {
            await ExecuteTestAndExpectError(sources: [@"Tests\Syntax\dbx_tests_syntax_singleprimitiveresult_invalidmarkup.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_FileResult()
        {
            await ExecuteTest(sources: [@"Tests\Syntax\dbx_tests_syntax_fileresult.sql"]).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_FileResultJsonGridResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_fileresultjson_gridresult.sql"]
              , contracts: [@"Contracts\Direction.json"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Inline_FileResultJsonMergeGridResult()
        {
            await ExecuteTest
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_fileresultjson_gridresult_merge.sql"]
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\JsonFileResultContract.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Enum1()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: [@"Tests\Syntax\dbx_tests_syntax_enum1.sql"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Enum2()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: [@"Tests\Syntax\dbx_tests_syntax_enum2.sql"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Enum3()
        {
            await ExecuteTest
            (
                isEmbedded: false
              , sources: [@"Tests\Syntax\dbx_tests_syntax_enum3.sql"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Client()
        {
            await ExecuteTest
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
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
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
                  , @"Contracts\NestedEnumerableContainer.json"
                  , @"Contracts\NestedEnumerableItem.json"
                  , @"Contracts\NestedEnumerableSubItem.json"
                }
              , endpoints: [@"Endpoints\GenericEndpoint.json"]
              , isEmbedded: false
              , outputKind: AssertOutputKind.Client
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Endpoints()
        {
            await ExecuteTest
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
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
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
                  , @"Contracts\NestedEnumerableContainer.json"
                  , @"Contracts\NestedEnumerableItem.json"
                  , @"Contracts\NestedEnumerableSubItem.json"
                }
              , endpoints: [@"Endpoints\GenericEndpoint.json"]
              , isEmbedded: false
              , outputKind: AssertOutputKind.Endpoint
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Endpoints_Accessor()
        {
            await ExecuteTest
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
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                }
              , endpoints: [@"Endpoints\GenericEndpointReflection.json"]
              , isEmbedded: false
              , outputKind: AssertOutputKind.Accessor
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Endpoints_Accessor_Model()
        {
            await ExecuteTest
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
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
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
                  , @"Contracts\NestedEnumerableContainer.json"
                  , @"Contracts\NestedEnumerableItem.json"
                  , @"Contracts\NestedEnumerableSubItem.json"
                }
              , endpoints: [@"Endpoints\GenericEndpoint.json"]
              , isEmbedded: false
              , outputKind: AssertOutputKind.Model
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Endpoints_OpenApi()
        {
            await ExecuteTest
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
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
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
                  , @"Contracts\NestedEnumerableContainer.json"
                  , @"Contracts\NestedEnumerableItem.json"
                  , @"Contracts\NestedEnumerableSubItem.json"
                }
              , endpoints: [@"Endpoints\GenericEndpoint.json"]
              , isEmbedded: false
              , outputKind: AssertOutputKind.OpenApi
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Endpoint_WithValidationErrors_Error()
        {
            await ExecuteTestAndExpectError
            (
                sources: new[]
                {
                    @"Types\dbx_codeanalysis_udt_generic.sql"
                  , @"Types\dbx_codeanalysis_udt_int.sql"
                  , @"Types\dbx_codeanalysis_udt_inttwo.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_empty_params_udt.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_fileresultjson_gridresult_merge_error.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult.sql"
                  , @"Tests\Syntax\dbx_tests_syntax_singleconcreteresult_missingcolumn.sql"
                }
              , contracts: new[]
                {
                    @"Contracts\Direction.json"
                  , @"Contracts\GenericContract.json"
                  , @"Contracts\InputContract.json"
                  , @"Contracts\Entry.json"
                  , @"Contracts\JsonFileResultContractError.json"
                  , @"Contracts\Request.json"
                  , @"Contracts\UnusedContract.json"
                }
              , endpoints: [@"Endpoints\GenericEndpointWithErrors.json"]
              , isEmbedded: false
            ).ConfigureAwait(false);
        }

        [TestMethod]
        [Ignore("Output parameters are now supported")]
        public async Task Endpoint_WithOutputParam_Error()
        {
            await ExecuteTestAndExpectError
            (
                sources: [@"Tests\Syntax\dbx_tests_syntax_empty_params_out.sql"]
              , endpoints: [@"Endpoints\GenericEndpointWithOutputParam.json"]
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DuplicateContract_Error()
        {
            await ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicateContract.json"
                  , @"Contracts\AccessRights.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DuplicatePropertyName_Error()
        {
            await ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicatePropertyName.json"
                  , @"Contracts\AccessRights.json"
                }
            ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DuplicatePropertyNameCaseInsensitive_Error()
        {
            await ExecuteTestAndExpectError
            (
                contracts: new[]
                {
                    @"Contracts\DuplicatePropertyNameCaseInsensitive.json"
                  , @"Contracts\AccessRights.json"
                }
            ).ConfigureAwait(false);
        }
    }
}