using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class TextTemplatingEngineErrorReporter : ErrorReporter, IErrorReporter
    {
        #region Fields
        private readonly ITextTemplatingEngineHost _textTemplatingEngineHost;
        #endregion

        #region Constructor
        public TextTemplatingEngineErrorReporter(ITextTemplatingEngineHost textTemplatingEngineHost)
        {
            this._textTemplatingEngineHost = textTemplatingEngineHost;
        }
        #endregion

        #region Overrides
        public override bool ReportErrors()
        {
            this._textTemplatingEngineHost.LogErrors(base.Errors);
            return base.HasErrors;
        }
        #endregion
    }
}