using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NeighborActionTarget : GeneratedAccessorMethodTarget
    {
        public override ICollection<ErrorResponse> ErrorResponses { get; }

        public NeighborActionTarget(string accessorFullName, TypeReference resultType, string operationName, bool isAsync) : base(accessorFullName, resultType, operationName, isAsync)
        {
            this.ErrorResponses = new Collection<ErrorResponse>();
        }
    }
}