namespace Dibix.Sdk.CodeGeneration
{
    public interface IDacPacSelectionExpression : ISourceConfigurationExpression
    {
        IDacPacSelectionExpression SelectProcedure(string procedureName, string displayName);
    }
}