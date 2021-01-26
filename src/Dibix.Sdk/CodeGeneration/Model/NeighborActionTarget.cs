using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class NeighborActionTarget : ActionDefinitionTarget
    {
        public override ICollection<ErrorResponse> ErrorResponses { get; }

        public NeighborActionTarget(string accessorFullName, string operationName, TypeReference resultType, bool isAsync) : base(accessorFullName, operationName, resultType, isAsync)
        {
            this.ErrorResponses = new Collection<ErrorResponse>();
        }
    }
}