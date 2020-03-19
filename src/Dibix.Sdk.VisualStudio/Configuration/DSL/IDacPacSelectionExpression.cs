namespace Dibix.Sdk.VisualStudio
{
    public interface IDacPacSelectionExpression : ISourceConfigurationExpression
    {
        IDacPacSelectionExpression SelectProcedure(string procedureName, string displayName);
    }
}