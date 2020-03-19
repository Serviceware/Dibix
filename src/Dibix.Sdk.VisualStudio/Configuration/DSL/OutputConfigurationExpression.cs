using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class OutputConfigurationExpression : IOutputConfigurationExpression
    {
        #region Fields
        private readonly CodeGenerationModel _model;
        #endregion

        #region Constructor
        public OutputConfigurationExpression(CodeGenerationModel model)
        {
            this._model = model;
        }
        #endregion

        #region IOutputConfigurationExpression Members
        public IOutputConfigurationExpression Formatting(CommandTextFormatting formatting)
        {
            this._model.CommandTextFormatting = formatting;
            return this;
        }

        public IOutputConfigurationExpression Namespace(string @namespace)
        {
            this._model.RootNamespace = @namespace;
            return this;
        }

        public IOutputConfigurationExpression ClassName(string className)
        {
            this._model.DefaultClassName = className;
            return this;
        }
        #endregion
    }
}