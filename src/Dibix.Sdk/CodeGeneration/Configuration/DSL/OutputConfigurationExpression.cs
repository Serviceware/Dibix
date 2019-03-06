namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OutputConfigurationExpression : IOutputConfigurationExpression
    {
        #region Fields
        private readonly OutputConfiguration _outputConfiguration;
        #endregion

        #region Constructor
        public OutputConfigurationExpression(OutputConfiguration outputConfiguration)
        {
            this._outputConfiguration = outputConfiguration;
        }
        #endregion

        #region IOutputConfigurationExpression Members
        public IOutputConfigurationExpression Formatting(CommandTextFormatting formatting)
        {
            this._outputConfiguration.Formatting = formatting;
            return this;
        }

        public IOutputConfigurationExpression Namespace(string @namespace)
        {
            this._outputConfiguration.Namespace = @namespace;
            return this;
        }

        public IOutputConfigurationExpression ClassName(string className)
        {
            this._outputConfiguration.ClassName = className;
            return this;
        }
        #endregion
    }
}