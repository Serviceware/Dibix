/*------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Dibix SDK 1.0.0.0.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//----------------------------------------------------------------------------*/
using System.Data;
using Dibix;

[assembly: ArtifactAssembly]

#region Accessor
namespace Dibix.Sdk.Tests.Data
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // EmptyWithOutputParam
        private const string EmptyWithOutputParamCommandText = "[dbo].[dbx_tests_syntax_empty_params_out]";

        public static short EmptyWithOutputParam(this IDatabaseAccessorFactory databaseAccessorFactory, out short a)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetInt16(nameof(a), out IOutParameter<short> aOutput)
                                                    .Build();
                short result = accessor.QuerySingle<short>(EmptyWithOutputParamCommandText, CommandType.StoredProcedure, @params);
                a = aOutput.Result;
                return result;
            }
        }
    }
}
#endregion