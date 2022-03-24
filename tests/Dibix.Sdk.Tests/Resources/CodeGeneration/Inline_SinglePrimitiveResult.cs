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
namespace Dibix.Sdk.Tests.Data.Extension.Primitive
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // GetSinglePrimitiveResult
        private const string GetSinglePrimitiveResultCommandText = "SELECT NEWID()";

        public static System.Guid GetSinglePrimitiveResult(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.QuerySingle<System.Guid>(GetSinglePrimitiveResultCommandText, CommandType.Text, ParametersVisitor.Empty);
            }
        }
    }
}
#endregion