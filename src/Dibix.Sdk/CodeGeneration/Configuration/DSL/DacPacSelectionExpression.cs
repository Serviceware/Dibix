namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSelectionExpression : SourceSelectionExpression<DacPacSelection>, IDacPacSelectionExpression, ISourceSelectionExpression
    {
        #region Constructor
        public DacPacSelectionExpression(IExecutionEnvironment environment, string packagePath) : base(new DacPacSelection(environment, packagePath)) { }
        #endregion

        #region IDacPacSelectionExpression Members
        public IDacPacSelectionExpression SelectProcedure(string procedureName, string displayName)
        {
            base.Configuration.AddStoredProcedure(procedureName, displayName);
            return this;
        }
        #endregion
    }
}