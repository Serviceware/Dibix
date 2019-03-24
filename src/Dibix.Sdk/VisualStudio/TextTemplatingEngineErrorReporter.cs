using System;
using System.CodeDom.Compiler;
using Dibix.Sdk.CodeGeneration;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.VisualStudio
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
            foreach (CompilerError error in base.Errors)
            {
                // Apparently errors are reported with distinct description, even though a different position is supplied
                // To make it work we append the position to the message
                error.ErrorText = String.Concat(error.ErrorText, ZeroWidthUtility.MaskText($" ({error.Line},{error.Column})"));
            }
            this._textTemplatingEngineHost.LogErrors(base.Errors);
            return base.HasErrors;
        }
        #endregion
    }
}