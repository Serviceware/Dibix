using System;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class TextTemplatingEngineErrorReporter : IErrorReporter
    {
        #region Fields
        private readonly ITextTemplatingEngineHost _textTemplatingEngineHost;
        private readonly CompilerErrorCollection _errors;
        #endregion

        #region Constructor
        public TextTemplatingEngineErrorReporter(ITextTemplatingEngineHost textTemplatingEngineHost)
        {
            this._textTemplatingEngineHost = textTemplatingEngineHost;
            this._errors = new CompilerErrorCollection();
        }
        #endregion

        #region IErrorReporter Members
        public void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            // Apparently errors are reported with distinct description, even though a different position is supplied
            // To make it work we append the position to the message
            errorText = String.Concat(errorText, ZeroWidthUtility.MaskText($" ({line},{column})"));
            this._errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
        }

        public bool ReportErrors()
        {
            this._textTemplatingEngineHost.LogErrors(this._errors);
            return this._errors.Count > 0;
        }
        #endregion
    }
}