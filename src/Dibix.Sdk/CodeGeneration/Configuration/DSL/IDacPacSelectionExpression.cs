namespace Dibix.Sdk.CodeGeneration
{
    public interface IDacPacSelectionExpression : ISourceSelectionExpression
    {
        IDacPacSelectionExpression SelectProcedure(string procedureName, string displayName);
    }
}