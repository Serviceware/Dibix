namespace Dibix.Sdk.CodeGeneration
{
    public class NeighborActionTarget : ActionDefinitionTarget
    {
        public NeighborActionTarget(string accessorFullName, string operationName, bool isAsync, bool hasRefParameters, string source, int line, int column) : base(accessorFullName, operationName, isAsync, hasRefParameters, source, line, column)
        {
        }
    }
}