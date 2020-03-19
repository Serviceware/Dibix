using System;
using System.CodeDom.Compiler;
using System.Linq;
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
        protected override void ReportErrors()
        {
            this._textTemplatingEngineHost.LogErrors(base.Errors.Aggregate(new CompilerErrorCollection(), (x, y) =>
            {
                // Apparently errors are reported with distinct description, even though a different position is supplied
                // To make it work we append the position to the message
                string normalizedText = String.Concat(y.Text, ZeroWidthUtility.MaskText($" ({y.Line},{y.Column})"));
                x.Add(new CompilerError(y.Source, y.Line, y.Column, y.Code, normalizedText));
                return x;
            }));
        }
        #endregion
    }
}