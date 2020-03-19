namespace Dibix.Sdk.VisualStudio
{
    internal sealed class DacPacSourceConfigurationExpression : SourceConfigurationExpression<DacPacSourceConfiguration>, IDacPacSelectionExpression, ISourceConfigurationExpression
    {
        #region Constructor
        public DacPacSourceConfigurationExpression(DacPacSourceConfiguration configuration) : base(configuration) { }
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