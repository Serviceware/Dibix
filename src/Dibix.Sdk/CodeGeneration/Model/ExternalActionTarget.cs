using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NeighborActionTarget : GeneratedAccessorMethodTarget
    {
        public override ICollection<ErrorResponse> ErrorResponses { get; }

        public NeighborActionTarget(string accessorFullName, TypeReference resultType, string methodName) : base(accessorFullName, resultType, methodName)
        {
            this.ErrorResponses = new Collection<ErrorResponse>();
        }
    }
}